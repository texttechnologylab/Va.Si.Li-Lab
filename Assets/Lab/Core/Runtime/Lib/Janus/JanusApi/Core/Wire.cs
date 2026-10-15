using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VaSiLi.Janus
{
    public static class JanusVerbs
    {
        public const string Create = "create";
        public const string Claim = "claim";
        public const string Destroy = "destroy";
        public const string Attach = "attach";
        public const string Detach = "detach";
        public const string Message = "message";
        public const string Trickle = "trickle";
        public const string Hangup = "hangup";
        public const string Keepalive = "keepalive";

        public const string Success = "success";
        public const string Ack = "ack";
        public const string Error = "error";
        public const string Event = "event";
        public const string Webrtcup = "webrtcup";
        public const string Media = "media";
        public const string Slowlink = "slowlink";
        public const string HangupIn = "hangup";
        public const string Detached = "detached";
        public const string Timeout = "timeout";
    }

    public class JanusEnvelope
    {
        [JsonProperty("janus")] public string Janus;
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("session_id")] public long SessionId;
        [JsonProperty("sender")] public long Sender;
        [JsonProperty("data")] public JObject Data;
        [JsonProperty("plugindata")] public JanusPluginData PluginData;
        [JsonProperty("jsep")] public Jsep Jsep;
        [JsonProperty("candidate")] public JObject Candidate;
        [JsonProperty("error")] public JanusErrorBody Error;
        [JsonProperty("reason")] public string Reason;
    }

    public class JanusPluginData
    {
        [JsonProperty("plugin")] public string Plugin;
        [JsonProperty("data")] public JObject Data;
    }

    public class JanusErrorBody
    {
        [JsonProperty("code")] public int Code;
        [JsonProperty("reason")] public string Reason;
    }

    public class Jsep
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("sdp")] public string Sdp;
        [JsonProperty("e2ee", NullValueHandling = NullValueHandling.Ignore)] public bool? E2ee;
        [JsonProperty("rid_order", NullValueHandling = NullValueHandling.Ignore)] public string RidOrder;
        [JsonProperty("force_relay", NullValueHandling = NullValueHandling.Ignore)] public bool? ForceRelay;
    }

    public class JanusCandidate
    {
        [JsonProperty("candidate", NullValueHandling = NullValueHandling.Ignore)] public string Candidate;
        [JsonProperty("sdpMid", NullValueHandling = NullValueHandling.Ignore)] public string SdpMid;
        [JsonProperty("sdpMLineIndex", NullValueHandling = NullValueHandling.Ignore)] public int? SdpMLineIndex;
        [JsonProperty("completed", NullValueHandling = NullValueHandling.Ignore)] public bool? Completed;
    }

    internal class JanusRequest
    {
        [JsonProperty("janus")] public string Janus;
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("session_id", NullValueHandling = NullValueHandling.Ignore)] public long? SessionId;
        [JsonProperty("handle_id", NullValueHandling = NullValueHandling.Ignore)] public long? HandleId;
        [JsonProperty("plugin", NullValueHandling = NullValueHandling.Ignore)] public string Plugin;
        [JsonProperty("opaque_id", NullValueHandling = NullValueHandling.Ignore)] public string OpaqueId;
        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)] public object Body;
        [JsonProperty("jsep", NullValueHandling = NullValueHandling.Ignore)] public Jsep Jsep;
        [JsonProperty("candidate", NullValueHandling = NullValueHandling.Ignore)] public JanusCandidate Candidate;
    }
}
