using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 전투 씬의 전체 화면 휴식 패널 (휴식 · 탈출 이벤트 노드), 지도처럼 상단 바는 덮지 않음
public class RestPanelController : MonoBehaviour
{
    private enum RestPanelMode
    {
        None,
        Rest,
        Event
    }

    [Header("기본 UI")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Button healButton;
    [FormerlySerializedAs("upgradeButton")]
    [SerializeField] private Button enhanceButton;
    [SerializeField] private Button escapeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private int healAmount = 20;
    // QA용: 켜면 rules.json 회복 비율(R-43, 최대 체력의 40%) 대신 위의 고정 회복량 사용
    [SerializeField] private bool overrideHealAmountFromInspector;
    [SerializeField] private string escapeSceneName = "LobbyScene";

    // 패널을 연 지도 · 전투 매니저 (Open에서 받음)
    private TrainingMapController trainingMap;
    private TrainingBattleManager battleManager;
    private bool actionResolved;
    private RestPanelMode panelMode;
    private List<RestDeckEnhanceCandidate> enhanceCandidates = new List<RestDeckEnhanceCandidate>();
    private bool isEscaping;
    private bool isFinalEventNode;

    public bool IsOpen => gameObject.activeInHierarchy;

    public static bool IsRestNode(TrainingMapNodeData node)
    {
        return node != null && ResolvePanelMode(node.nodeType) != RestPanelMode.None;
    }

    // 지도에서 고른 휴식 · 이벤트 노드로 패널을 엶 (열 때마다 상태 초기화)
    public bool Open(TrainingMapNodeData node, TrainingMapController map, TrainingBattleManager battle)
    {
        RestPanelMode mode = node != null ? ResolvePanelMode(node.nodeType) : RestPanelMode.None;
        if (mode == RestPanelMode.None)
        {
            Debug.LogWarning("[RestPanelController] 휴식 또는 이벤트 노드가 아닙니다");
            return false;
        }

        trainingMap = map;
        battleManager = battle;
        panelMode = mode;
        actionResolved = false;
        isEscaping = false;
        isFinalEventNode = panelMode == RestPanelMode.Event && IsTerminalNode(node);

        gameObject.SetActive(true);
        BindButtons();
        RefreshEnhanceCandidates();
        RefreshHpText();
        RefreshActionButtons();
        return true;
    }

    private void Close()
    {
        panelMode = RestPanelMode.None;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLocalizedTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedTexts;
    }

    // 언어 변경 시 체력 · 버튼 문구 갱신
    private void RefreshLocalizedTexts()
    {
        RefreshHpText();
        RefreshActionButtons();
    }

    public void OnClickHeal()
    {
        if (panelMode != RestPanelMode.Rest || actionResolved)
        {
            return;
        }

        if (!TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            Debug.LogWarning("[RestPanelController] 저장된 체력 정보가 없습니다");
            return;
        }

        int resolvedHealAmount = overrideHealAmountFromInspector
            ? healAmount
            : Mathf.FloorToInt(maxHp * Mathf.Max(0f, GameRules.Training.restHealRatio));
        int nextHp = Mathf.Clamp(hp + resolvedHealAmount, 0, maxHp);
        TrainingRunState.SetPlayerHealthState(nextHp, maxHp);
        RefreshHpText();

        CompleteRestAction();
    }

    public void OnClickNext()
    {
        if (panelMode == RestPanelMode.Event && isFinalEventNode)
        {
            return;
        }

        if (panelMode == RestPanelMode.Rest && !actionResolved)
        {
            return;
        }

        TrainingRunState.CompletePendingNode(true);
        Close();
        trainingMap.ShowMap();
    }

    public void OnClickEscape()
    {
        if (panelMode != RestPanelMode.Event || isEscaping)
        {
            return;
        }

        isEscaping = true;

        try
        {
            if (TrainingBattleManager.buildingDeck != null)
            {
                TrainingRunDeckPersistence.SaveRunDeckAsPermanentDeck(
                    TrainingBattleManager.buildingDeck,
                    SelectedButtonControl.selectedCharacterList,
                    "이벤트 탈출로 저장덱을 갱신했습니다");
            }
        }
        catch (System.Exception exception)
        {
            isEscaping = false;
            Debug.LogError($"[RestPanelController] 덱 저장에 실패했습니다. 현재 진행을 유지하며 다시 시도할 수 있습니다: {exception.Message}");
            return;
        }

        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.ResetRun();
        SceneManager.LoadScene(string.IsNullOrEmpty(escapeSceneName) ? "LobbyScene" : escapeSceneName);
    }

    private static RestPanelMode ResolvePanelMode(TrainingNodeType nodeType)
    {
        switch (nodeType)
        {
            case TrainingNodeType.Rest:
                return RestPanelMode.Rest;
            case TrainingNodeType.Event:
                return RestPanelMode.Event;
            default:
                return RestPanelMode.None;
        }
    }

    private void RefreshHpText()
    {
        if (TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            if (hpText != null)
            {
                hpText.text = GameText.Ui("ui.rest.hp", new Dictionary<string, object> { { "hp", hp }, { "maxHp", maxHp } });
            }

            // 상단 바 체력도 런 체력으로 갱신
            if (battleManager != null && battleManager.battleUI != null)
            {
                battleManager.battleUI.UpdateRunHealth(hp, maxHp);
            }
            return;
        }

        if (hpText != null)
        {
            hpText.text = GameText.Ui("ui.rest.hpUnknown");
        }
    }

    private void RefreshEnhanceCandidates()
    {
        enhanceCandidates = RestDeckEnhanceService.GetEnhanceableCandidates(TrainingBattleManager.buildingDeck);
    }

    private void RefreshActionButtons()
    {
        bool isRest = panelMode == RestPanelMode.Rest;
        bool isEvent = panelMode == RestPanelMode.Event;

        if (healButton != null)
        {
            healButton.gameObject.SetActive(isRest);
            healButton.interactable = isRest && !actionResolved;
        }

        if (enhanceButton != null)
        {
            enhanceButton.gameObject.SetActive(isRest);
            // 3단계에서 카드 선택 패널을 연결하기 전까지 비활성
            enhanceButton.interactable = false;
        }

        if (escapeButton != null)
        {
            escapeButton.gameObject.SetActive(isEvent);
            escapeButton.interactable = isEvent;
            if (isFinalEventNode)
            {
                SetButtonLabel(escapeButton, GameText.Ui("ui.rest.saveDeck"));
            }
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isFinalEventNode);
            nextButton.interactable = !isFinalEventNode && (isEvent || actionResolved);
            SetButtonLabel(nextButton, GameText.Ui(isEvent ? "ui.rest.continue" : "ui.rest.next"));
        }
    }

    private void CompleteRestAction()
    {
        actionResolved = true;
        RefreshEnhanceCandidates();
        RefreshActionButtons();
    }

    private void BindButtons()
    {
        if (healButton != null)
        {
            healButton.onClick.RemoveListener(OnClickHeal);
            healButton.onClick.AddListener(OnClickHeal);
        }

        if (escapeButton != null)
        {
            escapeButton.onClick.RemoveListener(OnClickEscape);
            escapeButton.onClick.AddListener(OnClickEscape);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnClickNext);
            nextButton.onClick.AddListener(OnClickNext);
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label ?? string.Empty;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label ?? string.Empty;
        }
    }

    private static bool IsTerminalNode(TrainingMapNodeData node)
    {
        return node == null || node.nextNodeIds == null || node.nextNodeIds.Count == 0;
    }
}
