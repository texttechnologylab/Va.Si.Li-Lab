using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VaSiLi.Janus
{
    public class JanusSession
    {
        public long Id { get; private set; }
        public JanusConnection Connection { get; }
        public bool IsValid { get; private set; } = true;

        public event EventHandler TimedOut;
        public event EventHandler Reclaimed;

        readonly Dictionary<long, JanusHandle> _handles = new Dictionary<long, JanusHandle>();
        readonly object _handlesLock = new object();

        internal JanusSession(JanusConnection connection, long id)
        {
            Connection = connection;
            Id = id;
        }

        public async Task<THandle> AttachAsync<THandle>(string opaqueId = null, CancellationToken cancellation = default)
            where THandle : JanusHandle, new()
        {
            var handle = new THandle();
            await AttachInternal(handle, opaqueId, cancellation).ConfigureAwait(false);
            return handle;
        }

        public async Task<JanusHandle> AttachAsync(string plugin, string opaqueId = null, CancellationToken cancellation = default)
        {
            var handle = new JanusHandle.Generic(plugin);
            await AttachInternal(handle, opaqueId, cancellation).ConfigureAwait(false);
            return handle;
        }

        async Task<THandle> AttachInternal<THandle>(THandle handle, string opaqueId, CancellationToken cancellation)
            where THandle : JanusHandle
        {
            handle.Initialize(this, opaqueId);
            var env = await Connection.SendRequestAsync(new JanusRequest
            {
                Janus = JanusVerbs.Attach,
                Plugin = handle.PluginName,
                OpaqueId = opaqueId,
                SessionId = Id
            }, cancellation).ConfigureAwait(false);
            if (env.Janus != JanusVerbs.Success || env.Data == null)
                throw new JanusException("Attach failed: " + env.Reason);
            var handleId = env.Data["id"]?.Value<long>() ?? 0;
            handle.SetHandleId(handleId);
            lock (_handlesLock) _handles[handleId] = handle;
            return handle;
        }

        public async Task DestroyAsync(CancellationToken cancellation = default)
        {
            if (!IsValid) return;
            try
            {
                await Connection.SendRequestAsync(new JanusRequest
                {
                    Janus = JanusVerbs.Destroy,
                    SessionId = Id
                }, cancellation).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored — session may already be gone
            }
            IsValid = false;
            Connection.RemoveSession(Id);
        }

        internal void DispatchToHandle(JanusEnvelope env)
        {
            JanusHandle handle;
            lock (_handlesLock)
            {
                if (!_handles.TryGetValue(env.Sender, out handle)) return;
            }
            handle.HandleEnvelope(env);
        }

        internal void RemoveHandle(long handleId)
        {
            lock (_handlesLock) _handles.Remove(handleId);
        }

        internal void MarkTimedOut()
        {
            IsValid = false;
            Connection.Dispatch(() => TimedOut?.Invoke(this, EventArgs.Empty));
        }

        internal async Task ReclaimAsync(CancellationToken cancellation = default)
        {
            // After a transport drop the previous session's handles (and their PeerConnections)
            // are dead. A successful Janus 'claim' would keep those old handles alive server-side
            // as ghosts — still occupying publisher slots and showing frozen feeds to peers until
            // they time out. So we always create a brand-new session, re-attach every handle to
            // it, and re-key the connection's session map to the new id.
            var oldId = Id;
            var fresh = await Connection.SendRequestAsync(new JanusRequest
            {
                Janus = JanusVerbs.Create,
                Transaction = JanusProtocol.NewTransactionId()
            }, cancellation).ConfigureAwait(false);
            if (fresh.Janus != JanusVerbs.Success || fresh.Data == null)
                throw new JanusException("Failed to recreate session after disconnect: " + fresh.Reason);
            Id = fresh.Data["id"]?.Value<long>() ?? 0;
            Connection.ReplaceSessionId(oldId, Id);

            IsValid = true;

            JanusHandle[] handles;
            lock (_handlesLock)
            {
                handles = new JanusHandle[_handles.Count];
                _handles.Values.CopyTo(handles, 0);
                _handles.Clear();
            }
            foreach (var h in handles)
            {
                try
                {
                    var attachEnv = await Connection.SendRequestAsync(new JanusRequest
                    {
                        Janus = JanusVerbs.Attach,
                        Plugin = h.PluginName,
                        OpaqueId = h.OpaqueId,
                        SessionId = Id,
                        Transaction = JanusProtocol.NewTransactionId()
                    }, cancellation).ConfigureAwait(false);
                    var newId = attachEnv.Data?["id"]?.Value<long>() ?? 0;
                    h.SetHandleId(newId);
                    lock (_handlesLock) _handles[newId] = h;
                    await h.RestoreAsync(cancellation).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Janus] Failed to restore handle for plugin {h.PluginName}: {ex.Message}");
                }
            }

            Connection.Dispatch(() => Reclaimed?.Invoke(this, EventArgs.Empty));
        }
    }
}
