using System;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
using Steamworks;
#endif

public enum SteamClientState
{
    NotStarted,
    Disabled,
    Ready,
    Failed,
    RestartRequired,
    Stopped
}

[DefaultExecutionOrder(-10000)]
public sealed class SteamClient : MonoBehaviour
{
    public static SteamClientState State { get; private set; }
    public static bool IsInitialized => State == SteamClientState.Ready;
    public static bool IsOnline { get; private set; }
    public static ulong UserId { get; private set; }
    public static string PersonaName { get; private set; } = string.Empty;
    public static string LastError { get; private set; } = string.Empty;
    public static event Action StatusChanged;

    private static SteamClient instance;
    private bool ownsApi;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
    private Callback<SteamServersConnected_t> connected;
    private Callback<SteamServersDisconnected_t> disconnected;
    private Callback<SteamServerConnectFailure_t> connectionFailed;
    private Callback<PersonaStateChange_t> personaChanged;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (instance != null)
        {
            instance.Shutdown();
        }

        instance = null;
        State = SteamClientState.NotStarted;
        IsOnline = false;
        UserId = 0;
        PersonaName = string.Empty;
        LastError = string.Empty;
        StatusChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (instance == null && State == SteamClientState.NotStarted)
        {
            new GameObject(nameof(SteamClient)).AddComponent<SteamClient>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            SteamSettings settings = SteamSettings.Load();
            if (!settings.ShouldInitialize(Application.isEditor))
            {
                State = SteamClientState.Disabled;
                Debug.Log("[SteamClient] Steam 연동을 사용하지 않는 개발 모드입니다");
                StatusChanged?.Invoke();
                return;
            }

            settings.Validate();
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            if (!Packsize.Test() || !DllCheck.Test())
            {
                throw new InvalidOperationException("Steam 네이티브 라이브러리와 래퍼의 버전 또는 구조가 일치하지 않습니다");
            }

            if (!Application.isEditor && settings.restartThroughSteam
                && SteamAPI.RestartAppIfNecessary(new AppId_t(settings.appId)))
            {
                State = SteamClientState.RestartRequired;
                StatusChanged?.Invoke();
                Application.Quit();
                return;
            }

            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                throw new InvalidOperationException($"Steam 초기화에 실패했습니다: {result}, {error}");
            }

            ownsApi = true;
            if (SteamUtils.GetAppID().m_AppId != settings.appId)
            {
                throw new InvalidOperationException("실행 중인 Steam App ID가 게임 설정과 다릅니다");
            }

            connected = Callback<SteamServersConnected_t>.Create(_ => RefreshIdentity());
            disconnected = Callback<SteamServersDisconnected_t>.Create(_ => RefreshIdentity());
            connectionFailed = Callback<SteamServerConnectFailure_t>.Create(_ => RefreshIdentity());
            personaChanged = Callback<PersonaStateChange_t>.Create(change =>
            {
                if (change.m_ulSteamID == UserId)
                {
                    RefreshIdentity();
                }
            });
            State = SteamClientState.Ready;
            RefreshIdentity(false);
            Debug.Log("[SteamClient] Steam 초기화가 완료되었습니다");
#else
            throw new PlatformNotSupportedException("현재 플랫폼에서는 Steam 연동을 지원하지 않습니다");
#endif
        }
        catch (Exception exception)
        {
            Shutdown();
            LastError = exception.Message;
            State = SteamClientState.Failed;
            Debug.LogError($"[SteamClient] {LastError}");
        }

        StatusChanged?.Invoke();
    }

    private void Update()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        if (ownsApi)
        {
            SteamAPI.RunCallbacks();
        }
#endif
    }

    private void RefreshIdentity(bool notify = true)
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        UserId = SteamUser.GetSteamID().m_SteamID;
        PersonaName = SteamFriends.GetPersonaName();
        IsOnline = SteamUser.BLoggedOn();
#endif
        if (notify)
        {
            StatusChanged?.Invoke();
        }
    }

    public static bool TryGetUser(out ulong userId, out string personaName)
    {
        userId = UserId;
        personaName = PersonaName;
        return IsInitialized && userId != 0;
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Shutdown();
            instance = null;
        }
    }

    public void Shutdown()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        connected?.Dispose();
        disconnected?.Dispose();
        connectionFailed?.Dispose();
        personaChanged?.Dispose();
        connected = null;
        disconnected = null;
        connectionFailed = null;
        personaChanged = null;
        if (ownsApi)
        {
            ownsApi = false;
            SteamAPI.Shutdown();
        }
#endif
        IsOnline = false;
        UserId = 0;
        PersonaName = string.Empty;
        State = SteamClientState.Stopped;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditorCleanup()
    {
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= ShutdownBeforeReload;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ShutdownBeforeReload;
    }

    private static void ShutdownBeforeReload()
    {
        if (instance != null)
        {
            instance.Shutdown();
        }
    }
#endif
}
