using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.WebRTC;
using UnityEngine;

namespace VaSiLi.Janus.WebRTC
{
    /// <summary>
    /// Buffers remote ICE candidates received from Janus via <see cref="JanusHandle.TrickleReceived"/>
    /// and applies them to a PeerConnection. Janus may start trickling before our SetRemoteDescription
    /// has resolved (its coroutine bridge runs async), so we hold candidates in a queue until the
    /// owning class signals readiness, then flush.
    /// </summary>
    internal class RemoteCandidateApplier
    {
        readonly string _logPrefix;
        readonly List<RTCIceCandidateInit> _pending = new List<RTCIceCandidateInit>();
        readonly object _lock = new object();
        RTCPeerConnection _pc;
        bool _ready;

        public RemoteCandidateApplier(string logPrefix) { _logPrefix = logPrefix; }

        /// <summary>Bind to a freshly-created PeerConnection. Resets readiness and discards any
        /// candidates buffered for the previous PC.</summary>
        public void Bind(RTCPeerConnection pc)
        {
            lock (_lock) { _pc = pc; _ready = false; _pending.Clear(); }
        }

        /// <summary>Call after SetRemoteDescription completes; flushes any buffered candidates and
        /// applies subsequent ones immediately.</summary>
        public void MarkReady()
        {
            List<RTCIceCandidateInit> toApply;
            lock (_lock)
            {
                _ready = true;
                if (_pending.Count == 0) return;
                toApply = new List<RTCIceCandidateInit>(_pending);
                _pending.Clear();
            }
            foreach (var init in toApply) TryAdd(init);
        }

        /// <summary>Feed in a Janus trickle payload (the JObject from JanusHandle.TrickleReceived).</summary>
        public void Receive(JObject candidate)
        {
            if (candidate == null) return;
            if (candidate["completed"]?.Value<bool?>() == true) return; // end-of-candidates signal
            var candStr = candidate["candidate"]?.Value<string>();
            if (string.IsNullOrEmpty(candStr)) return;
            var init = new RTCIceCandidateInit
            {
                candidate = candStr,
                sdpMid = candidate["sdpMid"]?.Value<string>(),
                sdpMLineIndex = candidate["sdpMLineIndex"]?.Value<int?>()
            };
            bool applyNow;
            lock (_lock)
            {
                if (_ready) applyNow = true;
                else { _pending.Add(init); applyNow = false; }
            }
            if (applyNow) TryAdd(init);
        }

        void TryAdd(RTCIceCandidateInit init)
        {
            var pc = _pc;
            if (pc == null) return;
            try
            {
                pc.AddIceCandidate(new RTCIceCandidate(init));
                Debug.Log($"{_logPrefix} applied remote ICE candidate: typ={UnityWebRtcExtensions.IceCandidateType(init.candidate)} mid={init.sdpMid}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{_logPrefix} AddIceCandidate failed: {ex.Message}");
            }
        }
    }
}
