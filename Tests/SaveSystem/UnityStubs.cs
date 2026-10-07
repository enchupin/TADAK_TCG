using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration, BeforeSceneLoad }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }
    public static class Application { public static string persistentDataPath; }
    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }
    public static class JsonUtility
    {
        public static string ToJson(object value, bool pretty) => JsonConvert.SerializeObject(value);
    }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        public static int GetInt(string key, int fallback) => Values.TryGetValue(key, out int value) ? value : fallback;
    }
}

public enum SteamClientState { NotStarted, Disabled, Ready, Failed }
public static class SteamClient
{
    public static SteamClientState State = SteamClientState.Disabled;
    public static ulong UserId;
    public static void EnsureInitialized() { }
    public static bool TryGetUser(out ulong id, out string name)
    {
        id = UserId;
        name = string.Empty;
        return State == SteamClientState.Ready && id != 0;
    }
}
public static class CharacterManager
{
    public static int GetIdByCharacterEnum(Character character) => (int)character;
}
