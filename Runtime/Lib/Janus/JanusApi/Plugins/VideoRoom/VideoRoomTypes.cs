using System.Collections.Generic;
using Newtonsoft.Json;

namespace VaSiLi.Janus.VideoRoom
{
    public static class VideoRoomEvents
    {
        public const string Joined = "joined";
        public const string Attached = "attached";
        public const string Event = "event";
        public const string Destroyed = "destroyed";
        public const string SlowLink = "slow_link";
        public const string Talking = "talking";
        public const string StoppedTalking = "stopped-talking";
        public const string Updated = "updated";
    }

    public static class VideoRoomPType
    {
        public const string Publisher = "publisher";
        public const string Subscriber = "subscriber";
    }

    // ---------- Requests ----------

    public class CreateRoomRequest
    {
        [JsonProperty("request")] public string Request = "create";
        [JsonProperty("room")] public long Room;
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)] public string Description;
        [JsonProperty("permanent", NullValueHandling = NullValueHandling.Ignore)] public bool? Permanent;
        [JsonProperty("publishers", NullValueHandling = NullValueHandling.Ignore)] public int? Publishers;
        [JsonProperty("bitrate", NullValueHandling = NullValueHandling.Ignore)] public int? Bitrate;
        [JsonProperty("fir_freq", NullValueHandling = NullValueHandling.Ignore)] public int? FirFreq;
        [JsonProperty("audiocodec", NullValueHandling = NullValueHandling.Ignore)] public string AudioCodec;
        [JsonProperty("videocodec", NullValueHandling = NullValueHandling.Ignore)] public string VideoCodec;
        [JsonProperty("notify_joining", NullValueHandling = NullValueHandling.Ignore)] public bool? NotifyJoining;
        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)] public bool? Record;
        [JsonProperty("rec_dir", NullValueHandling = NullValueHandling.Ignore)] public string RecDir;
        [JsonProperty("admin_key", NullValueHandling = NullValueHandling.Ignore)] public string AdminKey;
    }

    public class ExistsRequest
    {
        [JsonProperty("request")] public string Request = "exists";
        [JsonProperty("room")] public long Room;
    }

    public class DestroyRoomRequest
    {
        [JsonProperty("request")] public string Request = "destroy";
        [JsonProperty("room")] public long Room;
        [JsonProperty("permanent", NullValueHandling = NullValueHandling.Ignore)] public bool? Permanent;
    }

    public class JoinPublisherRequest
    {
        [JsonProperty("request")] public string Request = "join";
        [JsonProperty("room")] public long Room;
        [JsonProperty("ptype")] public string PType = VideoRoomPType.Publisher;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)] public long? Id;
    }

    public class JoinSubscriberRequest
    {
        [JsonProperty("request")] public string Request = "join";
        [JsonProperty("room")] public long Room;
        [JsonProperty("ptype")] public string PType = VideoRoomPType.Subscriber;
        [JsonProperty("streams", NullValueHandling = NullValueHandling.Ignore)] public List<StreamSubscription> Streams;
        [JsonProperty("private_id", NullValueHandling = NullValueHandling.Ignore)] public long? PrivateId;
        [JsonProperty("use_msid", NullValueHandling = NullValueHandling.Ignore)] public bool? UseMsid;
    }

    public class StreamSubscription
    {
        [JsonProperty("feed")] public long Feed;
        [JsonProperty("mid", NullValueHandling = NullValueHandling.Ignore)] public string Mid;
        [JsonProperty("crossrefid", NullValueHandling = NullValueHandling.Ignore)] public string CrossRefId;
    }

    public class ConfigureRequest
    {
        [JsonProperty("request")] public string Request = "configure";
        [JsonProperty("audio", NullValueHandling = NullValueHandling.Ignore)] public bool? Audio;
        [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)] public bool? Video;
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)] public bool? Data;
        [JsonProperty("bitrate", NullValueHandling = NullValueHandling.Ignore)] public int? Bitrate;
        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)] public bool? Record;
        [JsonProperty("filename", NullValueHandling = NullValueHandling.Ignore)] public string Filename;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
    }

    public class UnpublishRequest
    {
        [JsonProperty("request")] public string Request = "unpublish";
    }

    public class EnableRecordingRequest
    {
        [JsonProperty("request")] public string Request = "enable_recording";
        [JsonProperty("room")] public long Room;
        [JsonProperty("record")] public bool Record;
        [JsonProperty("secret", NullValueHandling = NullValueHandling.Ignore)] public string Secret;
    }

    public class StartSubscriberRequest
    {
        [JsonProperty("request")] public string Request = "start";
        [JsonProperty("room", NullValueHandling = NullValueHandling.Ignore)] public long? Room;
    }

    public class PauseRequest
    {
        [JsonProperty("request")] public string Request = "pause";
    }

    public class UpdateSubscriptionRequest
    {
        [JsonProperty("request")] public string Request = "update";
        [JsonProperty("subscribe", NullValueHandling = NullValueHandling.Ignore)] public List<StreamSubscription> Subscribe;
        [JsonProperty("unsubscribe", NullValueHandling = NullValueHandling.Ignore)] public List<StreamSubscription> Unsubscribe;
    }

    public class LeaveRequest
    {
        [JsonProperty("request")] public string Request = "leave";
    }

    // ---------- Responses / event payloads ----------

    public class VideoRoomEventData
    {
        [JsonProperty("videoroom")] public string Videoroom;
        [JsonProperty("room", NullValueHandling = NullValueHandling.Ignore)] public long? Room;
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)] public string Description;
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)] public long? Id;
        [JsonProperty("private_id", NullValueHandling = NullValueHandling.Ignore)] public long? PrivateId;
        [JsonProperty("publishers", NullValueHandling = NullValueHandling.Ignore)] public List<RemotePublisher> Publishers;
        [JsonProperty("attendees", NullValueHandling = NullValueHandling.Ignore)] public List<Attendee> Attendees;
        [JsonProperty("streams", NullValueHandling = NullValueHandling.Ignore)] public List<RemoteStream> Streams;
        [JsonProperty("leaving", NullValueHandling = NullValueHandling.Ignore)] public long? Leaving;
        [JsonProperty("unpublished", NullValueHandling = NullValueHandling.Ignore)] public long? Unpublished;
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)] public string Error;
        [JsonProperty("error_code", NullValueHandling = NullValueHandling.Ignore)] public int? ErrorCode;
        [JsonProperty("configured", NullValueHandling = NullValueHandling.Ignore)] public string Configured;
        [JsonProperty("started", NullValueHandling = NullValueHandling.Ignore)] public string Started;
        [JsonProperty("paused", NullValueHandling = NullValueHandling.Ignore)] public string Paused;
        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)] public bool? Record;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
    }

    public class RemotePublisher
    {
        [JsonProperty("id")] public long Id;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
        [JsonProperty("audio_codec", NullValueHandling = NullValueHandling.Ignore)] public string AudioCodec;
        [JsonProperty("video_codec", NullValueHandling = NullValueHandling.Ignore)] public string VideoCodec;
        [JsonProperty("dummy", NullValueHandling = NullValueHandling.Ignore)] public bool? Dummy;
        [JsonProperty("streams", NullValueHandling = NullValueHandling.Ignore)] public List<RemoteStream> Streams;
    }

    public class RemoteStream
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("mindex")] public int Mindex;
        [JsonProperty("mid")] public string Mid;
        [JsonProperty("codec", NullValueHandling = NullValueHandling.Ignore)] public string Codec;
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)] public string Description;
        [JsonProperty("disabled", NullValueHandling = NullValueHandling.Ignore)] public bool? Disabled;
        [JsonProperty("feed_id", NullValueHandling = NullValueHandling.Ignore)] public long? FeedId;
        [JsonProperty("feed_display", NullValueHandling = NullValueHandling.Ignore)] public string FeedDisplay;
        [JsonProperty("ready", NullValueHandling = NullValueHandling.Ignore)] public bool? Ready;
        [JsonProperty("send", NullValueHandling = NullValueHandling.Ignore)] public bool? Send;
    }

    public class Attendee
    {
        [JsonProperty("id")] public long Id;
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string Display;
    }
}
