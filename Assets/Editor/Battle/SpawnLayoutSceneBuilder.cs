using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스폰 배치 조정 씬 생성
/// 1920×1080 기준 캔버스에 우측 절반(몬스터 영역)만 배경색을 달리하고
/// 전투 씬의 스폰 위치 크기 · 파티 하단 높이를 읽어 같은 조건으로 구성
/// </summary>
public static class SpawnLayoutSceneBuilder
{
    private const string CombatScenePath = "Assets/Scenes/CombatScene.unity";
    private const string OutputScenePath = "Assets/Scenes/Test/SpawnLayoutScene.unity";
    private const string MonsterPrefabPath = "Assets/Prefab/Monster/Monster(Temp).prefab";
    private const string PartyObjectName = "PlayerPartyStanding";
    private const string SpawnerObjectName = "MonsterSpawner (1)";

    private static readonly Color ScreenColor = new(0.12f, 0.12f, 0.14f, 1f);
    private static readonly Color RegionColor = new(0.2f, 0.22f, 0.27f, 1f);

    [MenuItem("Tools/Battle/스폰 배치 씬 만들기")]
    public static void Build()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (monsterPrefab == null)
        {
            Debug.LogError($"[SpawnLayoutSceneBuilder] 몬스터 프리팹을 찾을 수 없습니다. path={MonsterPrefabPath}");
            return;
        }

        if (File.Exists(OutputScenePath)
            && !EditorUtility.DisplayDialog("스폰 배치 씬", $"{OutputScenePath}를 다시 만듭니다. 씬에서 직접 바꾼 내용은 사라집니다.", "다시 만들기", "취소"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!ReadCombatScene(out SpawnerRect spawner, out float partyBottomFromSpawn))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.tag = "MainCamera";

        GameObject canvasObject = new("SpawnLayoutCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // 화면 전체 배경, 우측 절반은 색으로만 구분
        CreateImage("Background", canvasObject.transform, Vector2.zero, Vector2.one, ScreenColor);
        CreateImage("MonsterRegion", canvasObject.transform, new Vector2(0.5f, 0f), Vector2.one, RegionColor);

        // 전투 씬 MonsterSpawnPositions와 같은 기준 (캔버스 우측 중앙)
        RectTransform spawnRoot = new GameObject("MonsterSpawnPositions", typeof(RectTransform)).GetComponent<RectTransform>();
        spawnRoot.SetParent(canvasObject.transform, false);
        spawnRoot.anchorMin = spawnRoot.anchorMax = new Vector2(1f, 0.5f);
        spawnRoot.pivot = new Vector2(1f, 0.5f);
        spawnRoot.sizeDelta = Vector2.zero;
        spawnRoot.anchoredPosition = Vector2.zero;

        SpawnLayoutPreview preview = spawnRoot.gameObject.AddComponent<SpawnLayoutPreview>();
        SerializedObject serialized = new(preview);
        serialized.FindProperty("monsterPrefab").objectReferenceValue = monsterPrefab;
        serialized.FindProperty("spawnRoot").objectReferenceValue = spawnRoot;
        serialized.FindProperty("spawnerAnchor").vector2Value = spawner.anchor;
        serialized.FindProperty("spawnerPivot").vector2Value = spawner.pivot;
        serialized.FindProperty("spawnerSize").vector2Value = spawner.size;
        serialized.FindProperty("partyBottomFromSpawn").floatValue = partyBottomFromSpawn;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(OutputScenePath));
        EditorSceneManager.SaveScene(scene, OutputScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[SpawnLayoutSceneBuilder] 스폰 배치 씬 생성 완료 → {OutputScenePath} (스폰 크기 {spawner.size}, 파티 하단 = 스폰 하단 + {partyBottomFromSpawn:0.#}), Game 창 1920×1080에서 플레이");
    }

    private static void CreateImage(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        Image image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private struct SpawnerRect
    {
        public Vector2 anchor;
        public Vector2 pivot;
        public Vector2 size;
    }

    // 전투 씬에서 스폰 위치 크기 · 앵커와 파티 하단 - 스폰 하단 높이 차를 읽음
    private static bool ReadCombatScene(out SpawnerRect spawner, out float partyBottomFromSpawn)
    {
        spawner = default;
        partyBottomFromSpawn = 0f;

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

                    if (spawn == null && rect.name == SpawnerObjectName)
                    {
                        spawn = rect;
                    }
                }
            }

            if (party == null || spawn == null)
            {
                Debug.LogError($"[SpawnLayoutSceneBuilder] 전투 씬에서 {PartyObjectName} 또는 {SpawnerObjectName}를 찾지 못했습니다");
                return false;
            }

            spawner.anchor = spawn.anchorMin;
            spawner.pivot = spawn.pivot;
            spawner.size = spawn.rect.size;

            Canvas canvas = party.GetComponentInParent<Canvas>(true).rootCanvas;
            float canvasScale = Mathf.Approximately(canvas.transform.lossyScale.y, 0f) ? 1f : canvas.transform.lossyScale.y;
            Vector3[] corners = new Vector3[4];
            party.GetWorldCorners(corners);
            float partyBottom = corners[0].y;
            spawn.GetWorldCorners(corners);
            float spawnBottom = corners[0].y;
            partyBottomFromSpawn = (partyBottom - spawnBottom) / canvasScale;
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
}
