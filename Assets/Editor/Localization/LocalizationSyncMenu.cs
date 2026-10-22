using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 로컬라이징 동기화 · 번역 상태 · CSV 메뉴
/// </summary>
public static class LocalizationSyncMenu
{
    private const string MenuRoot = "Tools/Localization/";

    [MenuItem(MenuRoot + "한국어 원문 동기화 (JsonData → ko)")]
    public static void SyncKorean()
    {
        LocalizationSyncReport report = LocalizationSourceSync.SyncKorean(GetProjectRoot());
        AssetDatabase.Refresh();
        Debug.Log($"[LocalizationSync] {report.ToSummary()}");
    }

    [MenuItem(MenuRoot + "번역 상태 보고")]
    public static void ReportStatus()
    {
        string root = GetProjectRoot();
        StringBuilder builder = new("[LocalizationSync] 번역 상태");
        foreach (string code in GetTargetLanguages(root))
        {
            List<TranslationRow> rows = LocalizationSourceSync.BuildRows(root, code);
            builder.Append($"\n{code}: ");
            builder.Append(string.Join(", ", rows.GroupBy(row => row.Status).OrderBy(group => group.Key).Select(group => $"{group.Key} {group.Count()}")));
        }

        Debug.Log(builder.ToString());
    }

    // 누락 · 인자 · 참조 · 복수형 · 남은 한글 · 조각 따옴표 · 토큰 잔여 검사
    [MenuItem(MenuRoot + "번역 검증")]
    public static void ValidateTranslations()
    {
        string root = GetProjectRoot();
        SortedDictionary<string, string> source = LocalizationSourceSync.ReadLanguage(root, LocalizationSourceSync.SourceLanguage);
        StringBuilder builder = new("[LocalizationSync] 번역 검증");
        int total = 0;
        foreach (string code in GetTargetLanguages(root))
        {
            List<LocalizationIssue> issues = LocalizationValidator.Validate(source, LocalizationSourceSync.ReadLanguage(root, code), code);
            total += issues.Count;
            builder.Append($"\n{code}: {issues.Count}건");
            foreach (LocalizationIssue issue in issues.Take(30))
            {
                builder.Append("\n  ").Append(issue);
            }
        }

        if (total == 0)
        {
            Debug.Log(builder.ToString());
        }
        else
        {
            Debug.LogWarning(builder.ToString());
        }
    }

    // 플레이 중 영어 문구를 악센트 · 40% 늘린 문구로 표시 ([[ ]] 없는 문구 = 하드코딩)
    [MenuItem(MenuRoot + "의사 언어 켜기 · 끄기 (플레이 중)")]
    public static void TogglePseudoLocalization()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("의사 언어", "플레이 모드에서만 사용할 수 있습니다.", "확인");
            return;
        }

        LocalizationManager.SetPseudoLocalization(!LocalizationManager.PseudoLocalizationEnabled);
        Debug.Log($"[LocalizationSync] 의사 언어 {(LocalizationManager.PseudoLocalizationEnabled ? "켜짐" : "꺼짐")}");
    }

    [MenuItem(MenuRoot + "CSV 내보내기 (모든 번역 언어)")]
    public static void ExportCsv()
    {
        string root = GetProjectRoot();
        foreach (string code in GetTargetLanguages(root))
        {
            LocalizationSourceSync.ExportCsv(root, code);
        }

        string folder = Path.Combine(root, LocalizationSourceSync.ExportFolder);
        Debug.Log($"[LocalizationSync] CSV 내보내기 완료: {folder}");
        EditorUtility.RevealInFinder(folder);
    }

    // 파일 이름(en.csv)으로 언어를 정함
    [MenuItem(MenuRoot + "CSV 가져오기")]
    public static void ImportCsv()
    {
        string root = GetProjectRoot();
        string path = EditorUtility.OpenFilePanel("번역 CSV 가져오기", Path.Combine(root, LocalizationSourceSync.ExportFolder), "csv");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string code = Path.GetFileNameWithoutExtension(path);
        if (!GetTargetLanguages(root).Contains(code))
        {
            EditorUtility.DisplayDialog("CSV 가져오기", $"파일 이름이 언어 코드가 아닙니다: {code}\n(예: en.csv, zh-Hans.csv)", "확인");
            return;
        }

        LocalizationSyncReport report = new();
        int imported = LocalizationSourceSync.ImportCsv(root, code, path, report);
        AssetDatabase.Refresh();
        Debug.Log($"[LocalizationSync] {code} {imported}개 반영{(report.Warnings.Count > 0 ? "\n" + string.Join("\n", report.Warnings) : string.Empty)}");
    }

    private static string GetProjectRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    // languages.json의 원문 외 언어 (비활성 포함)
    private static List<string> GetTargetLanguages(string root)
    {
        string path = Path.Combine(root, LocalizationSourceSync.LocalizationFolder, "languages.json");
        LocalizationLanguageList list = File.Exists(path)
            ? JsonConvert.DeserializeObject<LocalizationLanguageList>(File.ReadAllText(path, Encoding.UTF8))
            : null;
        return list?.languages?
            .Where(language => language != null && !string.IsNullOrWhiteSpace(language.code) && language.code != LocalizationSourceSync.SourceLanguage)
            .Select(language => language.code)
            .ToList() ?? new List<string>();
    }
}
