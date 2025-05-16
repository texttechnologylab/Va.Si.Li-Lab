using System;
using System.Collections.Generic;

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
        public string _id;
        public string shortName;
        public ApiLevel[] levels;
        public Dictionary<string, ApiRole> roles;
        [Obsolete("Currently not supported")]
        public int amountPlayersRequired;
    }

    [Serializable]
    public struct ApiLevel
    {
        public int id;
        public int delay;
        public Dictionary<string, ApiLevelRoleLocale> roleDescriptions;
    }

    [Serializable]
    public struct ApiRole
    {
        public string spawnPosition;
        public string id;
        public string mode;
        public bool admin;
        public int maxCount;
        public string[] restrictions;
        public Dictionary<string, ApiRoleLocale> locales;
        public bool IsSpectator => mode == Mode.Spectator;
        public bool IsPlayer => mode == Mode.Player;
        public bool IsWebCam => mode == Mode.WebCam;
        public bool IsAdmin => admin;
    }

    [Serializable]
    public struct ApiRoleLocale
    {
        public string name;
        public string[] description;
    }

    [Serializable]
    public struct ApiLevelRoleLocale
    {
        public Dictionary<string, ApiLevelRoleDescription> locales;
    }
    [Serializable]
    public struct ApiLevelRoleDescription
    {
        public string[] description;
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