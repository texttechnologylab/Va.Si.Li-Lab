using System.Collections.Generic;
using Newtonsoft.Json;

namespace VaSiLi.Janus.TextRoom
{
    public static class TextRoomActions
    {
        public const string Success = "success";
        public const string Error = "error";
        public const string Message = "message";
        public const string Announcement = "announcement";
        public const string Join = "join";
        public const string Leave = "leave";
        public const string Joined = "joined";
        public const string Left = "left";
        public const string Destroyed = "destroyed";
        public const string Kicked = "kicked";
    }

    // ---------- Plugin handle requests (sent via the Janus plugin handle, not the data channel) ----------

    public class TextRoomSetupRequest
    {
        [JsonProperty("request")] public string Request = "setup";
    }

    public class TextRoomAckRequest
    {
        [JsonProperty("request")] public string Request = "ack";
    }

    // ---------- Data channel messages (sent as JSON strings over the open data channel) ----------

    public class TextRoomCreateMessage
    {
        [JsonProperty("textroom")] public string Action = "create";
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("room")] public long Room;
        [JsonProperty("permanent", NullValueHandling = NullValueHandling.Ignore)] public bool? Permanent;
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)] public string Description;
        [JsonProperty("history", NullValueHandling = NullValueHandling.Ignore)] public int? History;
    }

    public class TextRoomDestroyMessage
    {
        [JsonProperty("textroom")] public string Action = "destroy";
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("room")] public long Room;
    }

    public class TextRoomJoinMessage
    {
        [JsonProperty("textroom")] public string Action = "join";
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("room")] public long Room;
        [JsonProperty("username")] public string Username;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
        [JsonProperty("history", NullValueHandling = NullValueHandling.Ignore)] public bool? History;
    }

    public class TextRoomLeaveMessage
    {
        [JsonProperty("textroom")] public string Action = "leave";
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("room")] public long Room;
    }

    public class TextRoomSendMessage
    {
        [JsonProperty("textroom")] public string Action = "message";
        [JsonProperty("transaction")] public string Transaction;
        [JsonProperty("room")] public long Room;
        [JsonProperty("text")] public string Text;
        [JsonProperty("ack", NullValueHandling = NullValueHandling.Ignore)] public bool? Ack;
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)] public string To;
        [JsonProperty("tos", NullValueHandling = NullValueHandling.Ignore)] public List<string> Tos;
    }

    // ---------- Incoming data channel events (parsed from received JSON) ----------

    public class TextRoomEventData
    {
        [JsonProperty("textroom")] public string Action;
        [JsonProperty("transaction", NullValueHandling = NullValueHandling.Ignore)] public string Transaction;
        [JsonProperty("room", NullValueHandling = NullValueHandling.Ignore)] public long? Room;
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)] public string From;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)] public string Text;
        [JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)] public string Date;
        [JsonProperty("whisper", NullValueHandling = NullValueHandling.Ignore)] public bool? Whisper;
        [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)] public string Username;
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)] public string Error;
        [JsonProperty("error_code", NullValueHandling = NullValueHandling.Ignore)] public int? ErrorCode;
        [JsonProperty("participants", NullValueHandling = NullValueHandling.Ignore)] public List<TextRoomParticipant> Participants;
    }

    public class TextRoomParticipant
    {
        [JsonProperty("username")] public string Username;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
    }
}
