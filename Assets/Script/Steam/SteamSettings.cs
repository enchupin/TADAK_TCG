using System;
using UnityEngine;

[Serializable]
public sealed class SteamSettings
{
    public bool enabled = true;
    public uint appId;
    public bool initializeInEditor;
    public bool restartThroughSteam = true;

    public bool ShouldInitialize(bool isEditor)
    {
        return enabled && (!isEditor || initializeInEditor);
    }

    public static SteamSettings Load()
    {
        TextAsset json = Resources.Load<TextAsset>("SteamSettings");
        if (json == null)
        {
            throw new InvalidOperationException("SteamSettings.json 설정 파일을 찾을 수 없습니다");
        }

        return JsonUtility.FromJson<SteamSettings>(json.text)
            ?? throw new InvalidOperationException("Steam 설정을 읽을 수 없습니다");
    }

    public void Validate()
    {
        if (enabled && appId == 0)
        {
            throw new InvalidOperationException("SteamSettings.json에 이 게임의 Steam App ID를 입력해야 합니다");
        }
    }
}
