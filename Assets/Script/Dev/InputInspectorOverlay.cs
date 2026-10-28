#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 개발용 입력 점검 오버레이 (F8로 켜고 끔)
/// 포인터 아래 UI · 2D 콜라이더, 활성 캔버스 정렬 순서, 전투 조작 허용 상태, 최근 클릭 · ESC 기록을 표시
/// 패널 입력 점검 작업이 끝나면 .claude-work로 옮겨 보관
/// </summary>
public sealed class InputInspectorOverlay : MonoBehaviour
{
    private const Key ToggleKey = Key.F8;
    private const int MaxUiHits = 8;
    private const int MaxLogEntries = 10;

    private static InputInspectorOverlay instance;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private readonly List<string> eventLog = new List<string>();
    private readonly StringBuilder builder = new StringBuilder();
    private PointerEventData pointerData;
    private GUIStyle textStyle;
    private Vector2 scroll;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("[InputInspectorOverlay]");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<InputInspectorOverlay>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[ToggleKey].wasPressedThisFrame)
        {
            visible = !visible;
        }

        // 표시 여부와 관계없이 기록해 두어 켠 직후에도 직전 입력을 확인할 수 있게 함
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            AddLog("클릭", DescribeTopUiHit() + " / 2D " + DescribeColliderHits(1));
        }
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            AddLog("ESC", "드래그 중=" + CardInteractionHandler.IsAnyCardDragging);
        }
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            TrainingBattleManager battle = TrainingBattleManager.Instance;
            AddLog("Space", battle != null ? "턴 종료 가능=" + battle.CanEndPlayerTurn() : "전투 아님");
        }
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true };
        }

        Rect area = new Rect(8f, 8f, Mathf.Min(560f, Screen.width - 16f), Mathf.Min(620f, Screen.height - 16f));
        GUI.Box(area, GUIContent.none);
        GUILayout.BeginArea(new Rect(area.x + 6f, area.y + 4f, area.width - 12f, area.height - 8f));
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(BuildReport(), textStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private string BuildReport()
    {
        builder.Clear();
        builder.AppendLine($"<b>입력 점검 (F8)</b>  씬: {SceneManager.GetActiveScene().name}");

        AppendBattleState();
        AppendUiHits();
        builder.AppendLine("<b>2D 콜라이더</b>  " + DescribeColliderHits(4));
        AppendCanvases();

        builder.AppendLine("<b>최근 입력</b>");
        for (int i = eventLog.Count - 1; i >= 0; i--)
        {
            builder.AppendLine("  " + eventLog[i]);
        }
        return builder.ToString();
    }

    private void AppendBattleState()
    {
        SettingsManager settings = SettingsManager.Instance;
        builder.AppendLine($"설정 패널: {(settings != null ? settings.IsSettingsPanelOpen().ToString() : "매니저 없음")}"
            + $"   카드 드래그 중: {CardInteractionHandler.IsAnyCardDragging}");

        TrainingBattleManager battle = TrainingBattleManager.Instance;
        if (battle == null)
        {
            return;
        }

        builder.AppendLine($"카드 사용: {Mark(battle.CanPlayerPlayCard())}   턴 종료: {Mark(battle.CanEndPlayerTurn())}"
            + $"   선택 대기: {battle.HasPendingSelection}   턴 상태: {battle.CurrentTurnState}");
    }

    private void AppendUiHits()
    {
        uiHits.Clear();
        EventSystem eventSystem = EventSystem.current;
        Mouse mouse = Mouse.current;
        if (eventSystem != null && mouse != null)
        {
            pointerData ??= new PointerEventData(eventSystem);
            pointerData.position = mouse.position.ReadValue();
            eventSystem.RaycastAll(pointerData, uiHits);
        }

        builder.AppendLine($"<b>포인터 아래 UI</b> ({uiHits.Count}개, 위에서부터)");
        for (int i = 0; i < uiHits.Count && i < MaxUiHits; i++)
        {
            GameObject hit = uiHits[i].gameObject;
            Canvas canvas = hit.GetComponentInParent<Canvas>();
            string canvasInfo = canvas != null ? $"{canvas.rootCanvas.name}#{canvas.rootCanvas.sortingOrder}" : "-";
            builder.AppendLine($"  {(i == 0 ? "▶" : " ")} {ShortPath(hit.transform)}  [{canvasInfo}]");
        }
    }

    private void AppendCanvases()
    {
        builder.AppendLine("<b>활성 루트 캔버스</b> (정렬 순서)");
        Dictionary<int, int> orderCounts = new Dictionary<int, int>();
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (!canvas.isRootCanvas || !canvas.isActiveAndEnabled)
            {
                continue;
            }
            orderCounts.TryGetValue(canvas.sortingOrder, out int count);
            orderCounts[canvas.sortingOrder] = count + 1;
        }

        foreach (Canvas canvas in canvases)
        {
            if (!canvas.isRootCanvas || !canvas.isActiveAndEnabled)
            {
                continue;
            }
            // 같은 정렬 순서의 캔버스가 함께 켜져 있으면 표시 (위아래가 계층 순서에 의존)
            string warn = orderCounts[canvas.sortingOrder] > 1 ? "  <color=#ff8080>순서 중복</color>" : string.Empty;
            builder.AppendLine($"  {canvas.name} #{canvas.sortingOrder}{warn}");
        }
    }

    private string DescribeTopUiHit()
    {
        uiHits.Clear();
        EventSystem eventSystem = EventSystem.current;
        Mouse mouse = Mouse.current;
        if (eventSystem == null || mouse == null)
        {
            return "UI 없음";
        }

        pointerData ??= new PointerEventData(eventSystem);
        pointerData.position = mouse.position.ReadValue();
        eventSystem.RaycastAll(pointerData, uiHits);
        return uiHits.Count > 0 ? "UI " + ShortPath(uiHits[0].gameObject.transform) : "UI 없음";
    }

    private static string DescribeColliderHits(int maxCount)
    {
        Camera camera = Camera.main;
        Mouse mouse = Mouse.current;
        if (camera == null || mouse == null)
        {
            return "-";
        }

        Vector2 worldPoint = camera.ScreenToWorldPoint(mouse.position.ReadValue());
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        if (hits.Length == 0)
        {
            return "없음";
        }

        StringBuilder text = new StringBuilder();
        for (int i = 0; i < hits.Length && i < maxCount; i++)
        {
            Monster monster = hits[i].GetComponentInParent<Monster>();
            text.Append(i > 0 ? ", " : string.Empty).Append(hits[i].name);
            if (monster != null)
            {
                text.Append($"(몬스터 {monster.name})");
            }
        }
        return text.ToString();
    }

    private void AddLog(string label, string detail)
    {
        eventLog.Add($"[{Time.frameCount}] {label}: {detail}");
        if (eventLog.Count > MaxLogEntries)
        {
            eventLog.RemoveAt(0);
        }
    }

    private static string ShortPath(Transform target)
    {
        // 마지막 세 단계 이름만 표시
        string path = target.name;
        Transform current = target.parent;
        for (int depth = 0; depth < 2 && current != null; depth++)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private static string Mark(bool value)
    {
        return value ? "<color=#80ff80>허용</color>" : "<color=#ff8080>차단</color>";
    }
}
#endif
