using System;

/// <summary>
/// Holds all the serializable types for the scene management system
/// </summary>
namespace VaSiLi.SceneManagement
{
    [Serializable]
    public struct ApiHeader<T>
    {
        public T[] result;
        public bool success;

    }

    [Serializable]
    public struct ApiHeaderNonArray<T>
    {
        public T result;
        public bool success;

    }

    [Serializable]
    public struct ApiScene
    {
        public string internalName;
        public string author;
        public bool enabled;
        public string name;
        public string id;
        public string shortName;
        public ApiLevel[] level;
        public int amountPlayersRequired;
    }

    [Serializable]
    public struct ApiLevel
    {
        public int id;
        public int delay;
    }

    [Serializable]
    public struct ApiRoleLanguages
    {
        public ApiRole[] EN;
        public ApiRole[] DE;
    }

    [Serializable]
    public struct ApiRole
    {
        public string spawnPosition;
        public string name;
        public ApiRoleDescription[] description;
        public ApiRoleDescription[] level;
        public string mode;
        public bool admin;
        public int maxCount;
        public string disability;
    }

    [Serializable]
    public struct ApiRoleDescription
    {
        public int id;
        public string description;
    }

    [Serializable]
    public struct ApiInfos
    {
        public string id;
        public string mode;
        public string description;
    }

    public static class Mode
    {
        public static string Spectator = "spectator";
        public static string Player = "player";
        public static string WebCam = "webcam";
    }
}