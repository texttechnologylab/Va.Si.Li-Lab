using System;
using System.Collections.Generic;

namespace VaSiLi.Logging
{
    /// <summary>
    /// Class to keep all the different message structures
    /// </summary>
    [Serializable]
    public class MessageBase
    {
        public string playerId;
        public int localTime;
    }

    [Serializable]
    public class PlayerMessage : MessageBase
    {
        public int messageId;
        public PlayerAudioMessage audioData;
        public PlayerBodyMessage body;
        public PlayerHandMessage leftHand;
        public PlayerHandMessage rightHand;

        public MetaMessage metaMessage;

        public List<int> count;
    }

    [Serializable]
    public struct PlayerBodyMessage
    {
        public List<PositionMessage> positions;
        public List<RotationMessage> rotations;
        public List<RotationMessage> cameraRotations;
        public List<PositionMessage> cameraPositions;
    }

    [Serializable]
    public struct PlayerHandMessage
    {
        public List<PositionMessage> positions;
        public List<RotationMessage> rotations;
    }

    [Serializable]
    public struct MetaMessage
    {
        public List<OVRPlugin.HandState> leftHandStates;
        public List<OVRPlugin.HandState> rightHandStates;
        public List<OVRPlugin.FaceState> faceStates;
        public List<OVRPlugin.EyeGazesState> eyeGazesStates;
    }

    [Serializable]
    public struct PlayerAudioMessage
    {
        public string base64;
    }

    [Serializable]
    public struct PositionMessage
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public struct RotationMessage
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [Serializable]
    public class ObjectMessage : MessageBase
    {
        public int referenceMessage;
        public string objectId;
        public string objectName;
        public string hand;
        public string interaction;
    }

    [Serializable]
    public class ButtonMessage : MessageBase
    {
        public int referenceMessage;
        public int buttonId;
        public string buttonName;
        public string mode;
    }

    [Serializable]
    public class LogMessage : MessageBase
    {
        public int referenceMessage;
        public string logMessage;
        public string stacktrace;
        public string logType;
    }

    [Serializable]
    public class MiscLogMessage: MessageBase
    {
        public object jsonData;
    }

    [Serializable]
    public class SpawnMessage
    {
        public string mode = "spawn";
        public int localTime;

        public string name;
        public string[] children;
        public string room;
        public string peer;
        public int origin;
    }

    [Serializable]
    public class PlayerLogin : MessageBase
    {
        public string roomId;
        public string sceneName;
        public string clientId;
        public int messageId;
    }

    [Serializable]
    public class PlayerRoleLogin : MessageBase
    {
        public string role;
        public int messageId;
    }


    [Serializable]
    public class LevelStatus : MessageBase
    {
        public string roomId;
        public string sceneName;
        public string clientId;
        public int levelID;
        public string levelStatus;
    }
}