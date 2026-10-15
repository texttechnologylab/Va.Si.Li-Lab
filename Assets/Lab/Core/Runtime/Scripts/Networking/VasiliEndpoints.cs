using System;
using System.Collections.Generic;
using Ubiq.Networking;
using UnityEngine;

namespace VaSiLi.Networking
{
    /// <summary>
    /// Addresses of every backend service Va.Si.Li-Lab talks to, loaded from a Resources folder.
    /// The real file is not part of the public repository: create your own with
    /// Tools > Va.Si.Li > Create Endpoints Config (or Assets > Create > VaSiLi > Endpoints) and keep
    /// it in any Resources folder. Without it, all services fall back to localhost and a warning is logged.
    /// </summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "VaSiLi/Endpoints", order = 0)]
    public class VasiliEndpoints : ScriptableObject
    {
        public const string ResourceName = "VasiliEndpoints";

        [Header("Ubiq")]
        [Tooltip("Server the RoomClient connects to. Empty: 127.0.0.1:8009 (local Ubiq server).")]
        public ConnectionDefinition ubiqServer;

        [Header("Va.Si.Li services (base URLs without trailing slash)")]
        [Tooltip("Database API: logging, scene definitions, tutorials, status checks.")]
        public string api = "http://localhost:8000";
        [Tooltip("Speech-to-text service used by the microphone listener and chatbot.")]
        public string speech = "http://localhost:8001";
        [Tooltip("LLM agent service.")]
        public string agent = "http://localhost:8002";
        [Tooltip("WhisperX / TTS service used by the ChatAPI component.")]
        public string speechSynthesis = "http://localhost:8003";
        [Tooltip("ShapeNet object search and download.")]
        public string shapeNet = "http://localhost:8004";
        [Tooltip("EventSystem template server.")]
        public string eventTemplates = "http://localhost:4567";

        [Header("InterView")]
        [Tooltip("Questionnaire page opened in the in-world browser. The interview token is appended as ?token=...&room=...")]
        public string questionnaire = "";
        [Tooltip("Follow-up questionnaire the interviewer can send to the interviewee after the interview. Empty: the follow-up button does nothing.")]
        public string followupQuestionnaire = "";

        [Header("WebRTC")]
        [Tooltip("Janus WebSocket endpoint.")]
        public string janus = "ws://localhost:8188";
        [Tooltip("STUN/TURN servers. Leave username/credential empty for STUN.")]
        public List<IceServer> iceServers = new();

        [Serializable]
        public class IceServer
        {
            public List<string> urls = new();
            public string username = "";
            public string credential = "";
        }

        private static VasiliEndpoints current;

        /// <summary>The project's endpoints, or localhost defaults if none are configured.</summary>
        public static VasiliEndpoints Current
        {
            get
            {
                if (current == null)
                    current = Load();
                return current;
            }
        }

        /// <summary>Ubiq server to connect to, falling back to a local Ubiq server.</summary>
        public ConnectionDefinition UbiqServerOrDefault()
        {
            if (ubiqServer != null)
                return ubiqServer;

            var local = CreateInstance<ConnectionDefinition>();
            local.sendToIp = "127.0.0.1";
            local.sendToPort = "8009";
            local.type = ConnectionType.TcpClient;
            return local;
        }

        private static VasiliEndpoints Load()
        {
            var endpoints = Resources.Load<VasiliEndpoints>(ResourceName);
            if (endpoints != null)
                return endpoints;

            Debug.LogWarning($"[VasiliEndpoints] No '{ResourceName}' asset found in a Resources folder, all services use localhost. " +
                             "Create one via Tools > Va.Si.Li > Create Endpoints Config.");
            return CreateInstance<VasiliEndpoints>();
        }
    }
}
