using System.Collections;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;

namespace VaSiLi.Janus.WebRTC
{
    /// <summary>
    /// Bridges Unity.WebRTC's coroutine-style async ops to Tasks. All bridges require a
    /// MonoBehaviour to drive the coroutine on the Unity main thread.
    /// </summary>
    public static class UnityWebRtcExtensions
    {
        public static Task<RTCSessionDescription> AsTask(this RTCSessionDescriptionAsyncOperation op, MonoBehaviour runner)
        {
            var tcs = new TaskCompletionSource<RTCSessionDescription>(TaskCreationOptions.RunContinuationsAsynchronously);
            runner.StartCoroutine(Wait(op, tcs));
            return tcs.Task;
        }

        public static Task AsTask(this RTCSetSessionDescriptionAsyncOperation op, MonoBehaviour runner)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            runner.StartCoroutine(Wait(op, tcs));
            return tcs.Task;
        }

        static IEnumerator Wait(RTCSessionDescriptionAsyncOperation op, TaskCompletionSource<RTCSessionDescription> tcs)
        {
            yield return op;
            if (op.IsError) tcs.TrySetException(new System.Exception(op.Error.message));
            else tcs.TrySetResult(op.Desc);
        }

        static IEnumerator Wait(RTCSetSessionDescriptionAsyncOperation op, TaskCompletionSource<bool> tcs)
        {
            yield return op;
            if (op.IsError) tcs.TrySetException(new System.Exception(op.Error.message));
            else tcs.TrySetResult(true);
        }

        public static Jsep ToJsep(this RTCSessionDescription desc)
        {
            return new Jsep
            {
                Type = desc.type == RTCSdpType.Offer ? "offer" : "answer",
                Sdp = desc.sdp
            };
        }

        public static RTCSessionDescription ToSessionDescription(this Jsep jsep)
        {
            var type = jsep.Type == "offer" ? RTCSdpType.Offer : RTCSdpType.Answer;
            return new RTCSessionDescription { type = type, sdp = jsep.Sdp };
        }

        /// <summary>
        /// Normalizes H.264 profile-level-id in an SDP so a Baseline offer (profile-iop "00")
        /// matches a decoder that only advertises Constrained Baseline ("e0").
        ///
        /// WebRTC negotiates H.264 by EXACT profile-level-id string match. Browsers (e.g. Edge)
        /// commonly offer "4200xx" (Baseline), while Unity/Android's hardware decoder advertises
        /// "42e0xx" (Constrained Baseline). They are decode-compatible — a "42e0" decoder can
        /// decode a "4200" stream (RFC 6184) — but the exact-match negotiation rejects the m-line
        /// (port 0) and no frames arrive. Rewriting "4200" -> "42e0" makes the match succeed
        /// without changing the actual level (the trailing level byte is preserved).
        /// </summary>
        public static string NormalizeH264ProfileLevelId(string sdp)
        {
            if (string.IsNullOrEmpty(sdp)) return sdp;
            // profile-level-id=4200XX  ->  profile-level-id=42e0XX   (XX = level, kept as-is)
            return System.Text.RegularExpressions.Regex.Replace(
                sdp,
                @"(profile-level-id=)42[0-9a-fA-F]{2}([0-9a-fA-F]{2})",
                "${1}42e0${2}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>Pull "host"/"srflx"/"prflx"/"relay" out of an ICE candidate SDP line for triage logging.</summary>
        public static string IceCandidateType(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return "end-of-candidates";
            const string marker = " typ ";
            var idx = candidate.IndexOf(marker, System.StringComparison.Ordinal);
            if (idx < 0) return "unknown";
            var rest = candidate.Substring(idx + marker.Length);
            var space = rest.IndexOf(' ');
            return space < 0 ? rest : rest.Substring(0, space);
        }

        /// <summary>Compact dump of SDP m= lines for triage logging without spamming the whole SDP.</summary>
        public static string MLineSummary(string sdp)
        {
            if (string.IsNullOrEmpty(sdp)) return "<empty>";
            var lines = sdp.Split('\n');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("m=", System.StringComparison.Ordinal))
                {
                    if (sb.Length > 0) sb.Append("  ||  ");
                    sb.Append(lines[i].TrimEnd('\r'));
                }
            }

            return sb.Length == 0 ? "<no m= lines>" : sb.ToString();
        }

        /// <summary>
        /// Per-m-line dump of codecs (a=rtpmap) with their a=fmtp params for triage logging.
        /// Lets us read the exact codec name + profile-level-id / packetization-mode that a
        /// remote (e.g. a browser) is offering, which a bare m= line PT list can't reveal.
        /// A "m=video ... 0" with no codecs below it means the m-line was rejected.
        /// </summary>
        public static string CodecSummary(string sdp)
        {
            if (string.IsNullOrEmpty(sdp)) return "<empty>";
            var lines = sdp.Split('\n');
            var sb = new System.Text.StringBuilder();

            // Collect a=fmtp lines per payload type so we can attach them to their rtpmap.
            var fmtpByPt = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');
                if (!line.StartsWith("a=fmtp:", System.StringComparison.Ordinal)) continue;
                var rest = line.Substring("a=fmtp:".Length);
                var sp = rest.IndexOf(' ');
                if (sp <= 0) continue;
                fmtpByPt[rest.Substring(0, sp)] = rest.Substring(sp + 1);
            }

            // Single line on purpose: Android logcat drops everything after the first '\n'
            // in a message, so a multi-line dump loses its body when captured via adb.
            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');
                if (line.StartsWith("m=", System.StringComparison.Ordinal))
                {
                    if (sb.Length > 0) sb.Append("  ||  ");
                    sb.Append(line);
                }
                else if (line.StartsWith("a=rtpmap:", System.StringComparison.Ordinal))
                {
                    var rest = line.Substring("a=rtpmap:".Length);
                    var sp = rest.IndexOf(' ');
                    var pt = sp > 0 ? rest.Substring(0, sp) : rest;
                    sb.Append(" | ").Append(rest);
                    if (fmtpByPt.TryGetValue(pt, out var fmtp))
                        sb.Append(" [").Append(fmtp).Append(']');
                }
            }

            return sb.Length == 0 ? "<no codecs>" : sb.ToString();
        }
    }
}
