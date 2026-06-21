using System;
using System.Collections.Generic;
using System.Linq;

namespace VaSiLi.WebCam
{
    public enum AckStatus { Pending, Acked, Failed }

    /// <summary>One tracked outbound message and its delivery state.</summary>
    public sealed class AckEntry
    {
        public string Id;
        public AckStatus Status;
        public long SentAt;       // Unix ms
        public bool IsNew;        // transient "just changed" flag, cleared by the view if it cares
        public Dictionary<string, object> Meta; // optional, e.g. label/count
    }

    /// <summary>
    /// Pure ack-correlation logic for answer-option batches: tracks sent messages by id,
    /// matches incoming acks, and fails messages that aren't acked within a timeout.
    ///
    /// Deliberately a plain class (not a MonoBehaviour) so it stays testable and
    /// transport-independent. A host MonoBehaviour drives <see cref="Tick"/> from Update()
    /// and subscribes to <see cref="Changed"/> to update its UI.
    /// </summary>
    public sealed class AckTracker
    {
        /// <summary>Fired (with the full ordered entry list) whenever any entry's state changes.</summary>
        public event Action<IReadOnlyList<AckEntry>> Changed;

        readonly Dictionary<string, AckEntry> _entries = new Dictionary<string, AckEntry>();
        readonly List<string> _order = new List<string>(); // insertion order

        /// <summary>Register a new pending message. Returns the generated msgId to put on the wire.</summary>
        public string Track(Dictionary<string, object> meta = null)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var id = $"ao_{now:x}_{Guid.NewGuid():N}";
            if (id.Length > 24) id = id.Substring(0, 24);

            _entries[id] = new AckEntry
            {
                Id = id,
                Status = AckStatus.Pending,
                SentAt = now,
                IsNew = true,
                Meta = meta
            };
            _order.Add(id);
            Emit();
            return id;
        }

        /// <summary>Mark a tracked message acked. Returns false if unknown or already acked.</summary>
        public bool Ack(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!_entries.TryGetValue(id, out var e) || e.Status == AckStatus.Acked) return false;
            e.Status = AckStatus.Acked;
            e.IsNew = true;
            Emit();
            return true;
        }

        /// <summary>
        /// Fail any pending message older than <paramref name="timeoutMs"/>. Call once per frame
        /// from the host's Update() — the Unity-idiomatic replacement for setTimeout, no threads.
        /// </summary>
        public void Tick(long timeoutMs = 5000)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            bool changed = false;
            foreach (var e in _entries.Values)
            {
                if (e.Status == AckStatus.Pending && now - e.SentAt >= timeoutMs)
                {
                    e.Status = AckStatus.Failed;
                    e.IsNew = true;
                    changed = true;
                }
            }
            if (changed) Emit();
        }

        /// <summary>The tracked entries in insertion order.</summary>
        public IReadOnlyList<AckEntry> List() => _order.Select(id => _entries[id]).ToList();

        void Emit() => Changed?.Invoke(List());
    }
}
