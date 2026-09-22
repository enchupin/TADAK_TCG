using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class SteamBuildTools : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    [MenuItem("Tools/Steam/에디터 테스트 App ID 파일 준비")]
    private static void PrepareEditorAppId()
    {
        SteamSettings settings = SteamSettings.Load();
        settings.Validate();
        if (!settings.enabled || settings.appId == 0)
        {
            throw new InvalidOperationException("먼저 Steam 설정과 실제 App ID를 지정해야 합니다");
        }

        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt"),
            settings.appId.ToString(System.Globalization.CultureInfo.InvariantCulture), new UTF8Encoding(false));
        Debug.Log("[SteamBuildTools] 에디터 테스트용 App ID 파일을 준비했습니다. Steam 실행 후 Unity를 다시 시작하세요");
    }

    [MenuItem("Tools/Steam/연동 상태 출력")]
    private static void PrintStatus()
    {
        Debug.Log($"[SteamBuildTools] 상태: {SteamClient.State}, 온라인: {SteamClient.IsOnline}, 사용자 ID: {SteamClient.UserId}, 닉네임: {SteamClient.PersonaName}, 오류: {SteamClient.LastError}");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        SteamSettings settings = SteamSettings.Load();
        if (!settings.enabled)
        {
            return;
        }

        try
        {
            settings.Validate();
        }
        catch (InvalidOperationException exception)
        {
            throw new BuildFailedException(exception.Message);
        }

        if (report.summary.platform != BuildTarget.StandaloneWindows64
            && report.summary.platform != BuildTarget.StandaloneOSX
            && report.summary.platform != BuildTarget.StandaloneLinux64)
        {
            throw new BuildFailedException("Steam 배포 빌드는 Windows 64비트, macOS 또는 Linux 64비트를 선택해야 합니다");
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (!SteamSettings.Load().enabled || (report.summary.options & BuildOptions.Development) != 0)
        {
            return;
        }

        string directory = Path.GetDirectoryName(report.summary.outputPath);
        if (Directory.GetFiles(directory, "steam_appid.txt", SearchOption.AllDirectories).Length > 0)
        {
            throw new BuildFailedException("배포 폴더에 테스트용 steam_appid.txt가 있습니다. 해당 파일을 배포 대상에서 제외하세요");
        }
    }
}
