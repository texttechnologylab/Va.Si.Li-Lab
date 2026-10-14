using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace VaSiLi.Janus.VideoRoom
{
    public class PublishersAddedEventArgs : EventArgs
    {
        public List<RemotePublisher> Publishers;
    }
    public class PublisherLeftEventArgs : EventArgs { public long Id; }
    public class StreamsAttachedEventArgs : EventArgs { public List<RemoteStream> Streams; }
    public class RoomDestroyedEventArgs : EventArgs { public long Room; }

    /// <summary>
    /// Strongly-typed handle for janus.plugin.videoroom.
    /// Awaitable methods drive request/response transactions; events fire for unsolicited notifications.
    /// </summary>
    public class VideoRoomHandle : JanusHandle
    {
        public const string Plugin = "janus.plugin.videoroom";

        public long Room { get; private set; }
        public string Display { get; private set; }
        public long? PublisherId { get; private set; }
        public long? PrivateId { get; private set; }
        public bool IsPublisher { get; private set; }
        public bool IsSubscriber { get; private set; }

        // State preserved across reconnects
        long? _restoreRoom;
        string _restoreDisplay;
        List<StreamSubscription> _restoreSubscriberStreams;
        bool _wasPublisher;
        bool _wasSubscriber;

        public event EventHandler<VideoRoomEventData> RoomJoined;
        public event EventHandler<VideoRoomEventData> SubscriberAttached;
        public event EventHandler<PublishersAddedEventArgs> PublishersAdded;
        public event EventHandler<PublisherLeftEventArgs> PublisherLeft;
        public event EventHandler<PublisherLeftEventArgs> PublisherUnpublished;
        public event EventHandler<StreamsAttachedEventArgs> OwnStreamsConfigured;
        public event EventHandler<RoomDestroyedEventArgs> RoomDestroyed;
        public event EventHandler<VideoRoomEventData> EventReceived;
        /// <summary>Fires when the server reports a change to the room's recording state.</summary>
        public event EventHandler<bool> RecordingChanged;

        /// <summary>Last known server-reported recording state, or null if never reported.</summary>
        public bool? IsRecording { get; private set; }

        /// <summary>Locally override the recording state (used when the server does not echo the state back to the sender).</summary>
        public void SetIsRecording(bool value) => IsRecording = value;

        public VideoRoomHandle()
        {
            PluginName = Plugin;
        }

        // ---------- High-level helpers ----------

        /// <summary>
        /// Idempotent: tries to create the room, ignoring "room exists" (error 427), then joins as publisher.
        /// </summary>
        public async Task<VideoRoomEventData> JoinOrCreateAsync(long room, string display, CreateRoomRequest createOptions = null, CancellationToken cancellation = default)
        {
            await EnsureRoomExistsAsync(room, createOptions, cancellation).ConfigureAwait(false);
            return await JoinPublisherAsync(room, display, cancellation).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries 'create'; swallows "already exists" (error 427) so the call is safe to make every time.
        /// </summary>
        public async Task EnsureRoomExistsAsync(long room, CreateRoomRequest options = null, CancellationToken cancellation = default)
        {
            var req = options ?? new CreateRoomRequest();
            req.Request = "create";
            req.Room = room;
            try
            {
                await SendMessageAsync(req, null, cancellation).ConfigureAwait(false);
            }
            catch (JanusException ex) when (ex.Code == 427)
            {
                // Room already exists — fine.
            }
        }

        public async Task<VideoRoomEventData> JoinPublisherAsync(long room, string display, CancellationToken cancellation = default)
        {
            var resp = await SendMessageAsync<VideoRoomEventData>(new JoinPublisherRequest
            {
                Room = room,
                Display = display
            }, null, cancellation).ConfigureAwait(false);

            if (resp.Data == null || resp.Data.Videoroom != VideoRoomEvents.Joined)
                throw new JanusException("Did not receive 'joined' from videoroom: " + resp.Data?.Error);

            Room = room;
            Display = display;
            PublisherId = resp.Data.Id;
            PrivateId = resp.Data.PrivateId;
            IsPublisher = true;
            _restoreRoom = room;
            _restoreDisplay = display;
            _wasPublisher = true;
            return resp.Data;
        }

        /// <summary>
        /// Sends configure with a publisher SDP offer; returns the SDP answer from the server.
        /// </summary>
        public async Task<Jsep> PublishAsync(Jsep offer, ConfigureRequest options = null, CancellationToken cancellation = default)
        {
            var req = options ?? new ConfigureRequest { Audio = true, Video = true };
            req.Request = "configure";
            var resp = await SendMessageAsync<VideoRoomEventData>(req, offer, cancellation).ConfigureAwait(false);
            if (resp.Jsep == null)
                throw new JanusException("Publisher configure did not return a JSEP answer");
            return resp.Jsep;
        }

        public Task UnpublishAsync(CancellationToken cancellation = default)
            => SendMessageAsync(new UnpublishRequest(), null, cancellation);

        /// <summary>
        /// Toggles room recording on/off. Server reflects the state via a Videoroom 'event' with
        /// a 'record' boolean which fires <see cref="RecordingChanged"/> on the publisher handle.
        /// </summary>
        public Task EnableRecordingAsync(long room, bool record, string secret = null, CancellationToken cancellation = default)
            => SendMessageAsync(new EnableRecordingRequest { Room = room, Record = record, Secret = secret }, null, cancellation);

        /// <summary>
        /// Subscribe handle: send 'join' as subscriber. Returns the SDP offer from the server.
        /// </summary>
        public async Task<Jsep> JoinSubscriberAsync(long room, List<StreamSubscription> streams, long? privateId = null, CancellationToken cancellation = default)
        {
            var resp = await SendMessageAsync<VideoRoomEventData>(new JoinSubscriberRequest
            {
                Room = room,
                Streams = streams,
                PrivateId = privateId,
                UseMsid = false
            }, null, cancellation).ConfigureAwait(false);

            Room = room;
            IsSubscriber = true;
            PrivateId = privateId;
            _restoreRoom = room;
            _restoreSubscriberStreams = streams;
            _wasSubscriber = true;
            if (resp.Jsep == null)
                throw new JanusException("Subscriber join did not return a JSEP offer");
            return resp.Jsep;
        }

        /// <summary>
        /// Send the SDP answer back to start the subscriber's media flow.
        /// </summary>
        public Task StartAsync(Jsep answer, CancellationToken cancellation = default)
            => SendMessageAsync(new StartSubscriberRequest { Room = Room == 0 ? (long?)null : Room }, answer, cancellation);

        public Task<JanusPluginEvent> UpdateSubscriptionAsync(List<StreamSubscription> add, List<StreamSubscription> remove, CancellationToken cancellation = default)
            => SendMessageAsync(new UpdateSubscriptionRequest { Subscribe = add, Unsubscribe = remove }, null, cancellation);

        public Task LeaveAsync(CancellationToken cancellation = default)
            => SendMessageAsync(new LeaveRequest(), null, cancellation);

        public Task PauseAsync(CancellationToken cancellation = default)
            => SendMessageAsync(new PauseRequest(), null, cancellation);

        // ---------- Low-level event dispatch ----------

        protected override void OnMessage(JanusEnvelope env)
        {
            if (env.PluginData?.Data == null) return;
            VideoRoomEventData data;
            try { data = env.PluginData.Data.ToObject<VideoRoomEventData>(); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VideoRoom] Failed to parse event data: {ex.Message}");
                return;
            }

            EventReceived?.Invoke(this, data);

            if (data.Record.HasValue)
            {
                IsRecording = data.Record.Value;
                RecordingChanged?.Invoke(this, data.Record.Value);
            }

            switch (data.Videoroom)
            {
                case VideoRoomEvents.Joined:
                    RoomJoined?.Invoke(this, data);
                    if (data.Publishers != null && data.Publishers.Count > 0)
                        PublishersAdded?.Invoke(this, new PublishersAddedEventArgs { Publishers = data.Publishers });
                    break;
                case VideoRoomEvents.Attached:
                    SubscriberAttached?.Invoke(this, data);
                    break;
                case VideoRoomEvents.Event:
                    if (data.Publishers != null && data.Publishers.Count > 0)
                        PublishersAdded?.Invoke(this, new PublishersAddedEventArgs { Publishers = data.Publishers });
                    if (data.Leaving.HasValue)
                        PublisherLeft?.Invoke(this, new PublisherLeftEventArgs { Id = data.Leaving.Value });
                    if (data.Unpublished.HasValue)
                        PublisherUnpublished?.Invoke(this, new PublisherLeftEventArgs { Id = data.Unpublished.Value });
                    if (data.Streams != null && data.Streams.Count > 0)
                        OwnStreamsConfigured?.Invoke(this, new StreamsAttachedEventArgs { Streams = data.Streams });
                    break;
                case VideoRoomEvents.Destroyed:
                    RoomDestroyed?.Invoke(this, new RoomDestroyedEventArgs { Room = data.Room ?? 0 });
                    break;
            }
        }

        public override async Task RestoreAsync(CancellationToken cancellation = default)
        {
            if (_wasPublisher && _restoreRoom.HasValue)
            {
                try { await JoinPublisherAsync(_restoreRoom.Value, _restoreDisplay, cancellation).ConfigureAwait(false); }
                catch (Exception ex) { Debug.LogError($"[VideoRoom] Failed to rejoin as publisher: {ex.Message}"); }
            }
            // Subscriber restore: JanusVideoRoomSubscriber.OnHandleRestored drives the full
            // JoinSubscriberAsync + SDP exchange. Calling JoinSubscriberAsync here would produce a
            // first join whose JSEP offer is silently discarded, then the second join from
            // OnHandleRestored would fail because the handle is already in subscriber mode.
            await base.RestoreAsync(cancellation).ConfigureAwait(false);
        }
    }
}
