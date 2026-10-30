using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 화면비 작업용 점검 도구 (작업 후 .claude-work로 보관)
// 열린 씬 · 프리팹 편집 화면의 루트 캔버스가 1920×1080 · Expand 기준인지 확인하고 일괄 적용
public static class AspectRatioCanvasTool
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    [MenuItem("Tools/화면비/캔버스 점검")]
    private static void Inspect()
    {
        StringBuilder report = new StringBuilder("[화면비] 루트 캔버스 점검\n");
        int mismatchCount = 0;
        foreach (Canvas canvas in FindRootCanvases())
        {
            bool ok = IsExpected(canvas.GetComponent<CanvasScaler>());
            if (!ok) mismatchCount++;
            report.AppendLine($"{(ok ? "정상" : "변경 필요")} | {Describe(canvas)}");
        }

        report.AppendLine("[화면비] stretch 배경 (비율 유지 확대 대상 후보)");
        foreach (Image image in FindStretchedBackgrounds())
        {
            report.AppendLine($"- {GetPath(image.transform)}");
        }

        report.Insert(0, $"변경 필요 {mismatchCount}개\n");
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/화면비/캔버스 기준 적용")]
    private static void Apply()
    {
        int changedCount = 0;
        foreach (Canvas canvas in FindRootCanvases())
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || IsExpected(scaler))
            {
                continue;
            }

            Undo.RecordObject(scaler, "화면비 캔버스 기준 적용");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            EditorUtility.SetDirty(scaler);
            MarkDirty(canvas.gameObject);
            changedCount++;
            Debug.Log($"[화면비] 적용: {Describe(canvas)}");
        }

        Debug.Log($"[화면비] 캔버스 {changedCount}개 적용, 씬 · 프리팹을 저장하세요");
    }

    private static bool IsExpected(CanvasScaler scaler)
    {
        return scaler != null
            && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.referenceResolution == ReferenceResolution
            && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand;
    }

    private static string Describe(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        string scalerText = scaler == null
            ? "CanvasScaler 없음"
            : $"{scaler.uiScaleMode} {scaler.referenceResolution.x}×{scaler.referenceResolution.y} {scaler.screenMatchMode}";
        return $"{canvas.gameObject.scene.name} / {GetPath(canvas.transform)} (정렬 {canvas.sortingOrder}) - {scalerText}";
    }

    private static List<Canvas> FindRootCanvases()
    {
        List<Canvas> result = new List<Canvas>();
        foreach (GameObject root in FindRoots())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                // 하위 캔버스의 CanvasScaler는 쓰이지 않으므로 루트만
                if (canvas.transform.parent == null || canvas.transform.parent.GetComponentInParent<Canvas>(true) == null)
                {
                    result.Add(canvas);
                }
            }
        }

        return result;
    }

    private static List<Image> FindStretchedBackgrounds()
    {
        List<Image> result = new List<Image>();
        foreach (GameObject root in FindRoots())
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.name.ToLowerInvariant().Contains("background") || image.GetComponent<AspectCoverImage>() != null)
                {
                    continue;
                }

                RectTransform rect = image.rectTransform;
                if (rect.anchorMin.x == 0f && rect.anchorMax.x == 1f)
                {
                    result.Add(image);
                }
            }
        }

        return result;
    }

    private static IEnumerable<GameObject> FindRoots()
    {
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            yield return prefabStage.prefabContentsRoot;
            yield break;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                yield return root;
            }
        }
    }

    private static void MarkDirty(GameObject target)
    {
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            return;
        }

        EditorSceneManager.MarkSceneDirty(target.scene);
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;
        for (Transform parent = target.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }

        return path;
    }
}
