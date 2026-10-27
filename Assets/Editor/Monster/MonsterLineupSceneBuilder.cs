using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 몬스터 크기 비교 씬 생성
/// 전투 씬의 캔버스 배율 설정과 파티 하단 높이를 읽어 같은 조건으로 구성
/// </summary>
public static class MonsterLineupSceneBuilder
{
    private const string CombatScenePath = "Assets/Scenes/CombatScene.unity";
    private const string OutputScenePath = "Assets/Scenes/Test/MonsterLineupScene.unity";
    private const string MonsterPrefabPath = "Assets/Prefab/Monster/Monster(Temp).prefab";
    private const string PartyObjectName = "PlayerPartyStanding";

    [MenuItem("Tools/Monster/몬스터 비교 씬 만들기")]
    public static void Build()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (monsterPrefab == null)
        {
            Debug.LogError($"[MonsterLineupSceneBuilder] 몬스터 프리팹을 찾을 수 없습니다. path={MonsterPrefabPath}");
            return;
        }

        if (File.Exists(OutputScenePath)
            && !EditorUtility.DisplayDialog("몬스터 비교 씬", $"{OutputScenePath}를 다시 만듭니다. 씬에서 직접 바꾼 내용은 사라집니다.", "다시 만들기", "취소"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!ReadCombatScene(out float partyBottomFromSpawn, out CanvasScalerSettings scaler))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        camera.orthographic = true;
        camera.tag = "MainCamera";

        GameObject canvasObject = new("LineupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        scaler.ApplyTo(canvasObject.GetComponent<CanvasScaler>());

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject lineupObject = new("MonsterLineup", typeof(RectTransform));
        RectTransform lineupRect = lineupObject.GetComponent<RectTransform>();
        lineupRect.SetParent(canvasObject.transform, false);
        lineupRect.anchorMin = Vector2.zero;
        lineupRect.anchorMax = Vector2.one;
        lineupRect.offsetMin = lineupRect.offsetMax = Vector2.zero;

        MonsterLineupPreview preview = lineupObject.AddComponent<MonsterLineupPreview>();
        SerializedObject serialized = new(preview);
        serialized.FindProperty("monsterPrefab").objectReferenceValue = monsterPrefab;
        serialized.FindProperty("partyBottomFromSpawn").floatValue = partyBottomFromSpawn;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(OutputScenePath));
        EditorSceneManager.SaveScene(scene, OutputScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[MonsterLineupSceneBuilder] 비교 씬 생성 완료 → {OutputScenePath} (파티 하단 = 스폰 하단 + {partyBottomFromSpawn:0.#}), 플레이해서 확인");
    }

    // 전투 씬에서 파티 하단과 몬스터 스폰 위치 하단의 높이 차, 캔버스 배율 설정을 읽음
    private static bool ReadCombatScene(out float partyBottomFromSpawn, out CanvasScalerSettings scaler)
    {
        partyBottomFromSpawn = 0f;
        scaler = default;

        Scene combatScene = SceneManager.GetSceneByPath(CombatScenePath);
        bool openedHere = !combatScene.isLoaded;
        if (openedHere)
        {
            combatScene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Additive);
        }

        try
        {
            RectTransform party = null;
            RectTransform spawn = null;
            foreach (GameObject root in combatScene.GetRootGameObjects())
            {
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (party == null && rect.name == PartyObjectName)
                    {
                        party = rect;
                    }
                }

                MonsterStateController monster = root.GetComponentInChildren<MonsterStateController>(true);
                if (spawn == null && monster != null)
                {
                    spawn = (RectTransform)monster.transform;
                }
            }

            if (party == null || spawn == null)
            {
                Debug.LogError($"[MonsterLineupSceneBuilder] 전투 씬에서 {PartyObjectName} 또는 몬스터 스폰 위치를 찾지 못했습니다");
                return false;
            }

            Canvas canvas = party.GetComponentInParent<Canvas>(true).rootCanvas;
            float canvasScale = Mathf.Approximately(canvas.transform.lossyScale.y, 0f) ? 1f : canvas.transform.lossyScale.y;
            Vector3[] corners = new Vector3[4];
            party.GetWorldCorners(corners);
            float partyBottom = corners[0].y;
            spawn.GetWorldCorners(corners);
            float spawnBottom = corners[0].y;
            partyBottomFromSpawn = (partyBottom - spawnBottom) / canvasScale;

            CanvasScaler sourceScaler = canvas.GetComponent<CanvasScaler>();
            scaler = sourceScaler != null ? CanvasScalerSettings.From(sourceScaler) : CanvasScalerSettings.Default;
            return true;
        }
        finally
        {
            if (openedHere)
            {
                EditorSceneManager.CloseScene(combatScene, true);
            }
        }
    }

    private struct CanvasScalerSettings
    {
        public CanvasScaler.ScaleMode mode;
        public Vector2 referenceResolution;
        public CanvasScaler.ScreenMatchMode matchMode;
        public float match;
        public float scaleFactor;
        public float referencePixelsPerUnit;

        public static CanvasScalerSettings Default => new()
        {
            mode = CanvasScaler.ScaleMode.ConstantPixelSize,
            referenceResolution = new Vector2(800f, 600f),
            scaleFactor = 1f,
            referencePixelsPerUnit = 100f
        };

        public static CanvasScalerSettings From(CanvasScaler source) => new()
        {
            mode = source.uiScaleMode,
            referenceResolution = source.referenceResolution,
            matchMode = source.screenMatchMode,
            match = source.matchWidthOrHeight,
            scaleFactor = source.scaleFactor,
            referencePixelsPerUnit = source.referencePixelsPerUnit
        };

        public void ApplyTo(CanvasScaler target)
        {
            target.uiScaleMode = mode;
            target.referenceResolution = referenceResolution;
            target.screenMatchMode = matchMode;
            target.matchWidthOrHeight = match;
            target.scaleFactor = scaleFactor;
            target.referencePixelsPerUnit = referencePixelsPerUnit;
        }
    }
}
