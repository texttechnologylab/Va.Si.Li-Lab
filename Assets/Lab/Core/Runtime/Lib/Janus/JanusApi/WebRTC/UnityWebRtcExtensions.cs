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
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(lines[i].TrimEnd('\r'));
                }
            }
            return sb.Length == 0 ? "<no m= lines>" : sb.ToString();
        }
    }
}
