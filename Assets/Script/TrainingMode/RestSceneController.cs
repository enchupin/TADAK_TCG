using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RestSceneController : MonoBehaviour
{
    private enum RestSceneMode
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
    [SerializeField] private string escapeSceneName = "LobbyScene";

    [Header("강화 목록")]
    [FormerlySerializedAs("upgradeScrollView")]
    [SerializeField] private ScrollRect enhanceScrollView;
    [FormerlySerializedAs("upgradeListContent")]
    [SerializeField] private RectTransform enhanceListContent;

    [Header("강화 카드 프리팹")]
    [SerializeField] private GameObject enhanceCardPrefab;
    [SerializeField] private float enhanceCardScale = 0.65f;
    [SerializeField] private RectTransform[] enhanceSlotAnchors = new RectTransform[8];

    private bool actionResolved;
    private RestSceneMode sceneMode;
    private List<RestDeckEnhanceCandidate> enhanceCandidates = new List<RestDeckEnhanceCandidate>();
    private RestSceneEnhanceSlotLayoutView enhanceListView;
    private bool isEscaping;
    private bool isFinalEventNode;

    public static bool IsEventEscapeContextActive { get; private set; }

    private void Start()
    {
        if (!TryInitializeScene(out TrainingMapNodeData pendingNode))
        {
            ReturnToMap();
            return;
        }

        sceneMode = ResolveSceneMode(pendingNode.nodeType);
        isFinalEventNode = sceneMode == RestSceneMode.Event && IsTerminalNode(pendingNode);
        IsEventEscapeContextActive = sceneMode == RestSceneMode.Event;
        EnsureRunDeck();

        enhanceListView = new RestSceneEnhanceSlotLayoutView(
            enhanceScrollView,
            enhanceListContent,
            enhanceCardPrefab,
            enhanceCardScale,
            enhanceSlotAnchors);

        BindButtons();
        enhanceListView.Configure();

        RefreshEnhanceCandidates();
        RefreshHpText();
        RefreshActionButtons();
    }

    private void Update()
    {
        if (!IsEventEscapeContextActive || isEscaping || Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        OnClickEscape();
    }

    private void OnDestroy()
    {
        IsEventEscapeContextActive = false;
    }

    public void OnClickHeal()
    {
        if (sceneMode != RestSceneMode.Rest || actionResolved)
        {
            return;
        }

        if (!TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            Debug.LogWarning("[RestSceneController] 저장된 체력 정보가 없습니다");
            return;
        }

        int nextHp = Mathf.Clamp(hp + healAmount, 0, maxHp);
        TrainingRunState.SetPlayerHealthState(nextHp, maxHp);
        RefreshHpText();

        CompleteRestAction();
    }

    public void OnClickEnhance()
    {
        if (sceneMode != RestSceneMode.Rest || actionResolved)
        {
            return;
        }

        RefreshEnhanceCandidates();
        if (enhanceCandidates.Count <= 0)
        {
            enhanceListView.Hide();
            RefreshActionButtons();
            return;
        }

        if (!enhanceListView.Show(enhanceCandidates, ApplySelectedEnhance))
        {
            enhanceListView.Hide();
        }
    }

    public void OnClickNext()
    {
        if (sceneMode == RestSceneMode.Event && isFinalEventNode)
        {
            return;
        }

        if (sceneMode == RestSceneMode.Rest && !actionResolved)
        {
            return;
        }

        TrainingRunState.CompletePendingNode(true);
        ReturnToMap();
    }

    public void OnClickEscape()
    {
        if (sceneMode != RestSceneMode.Event || isEscaping)
        {
            return;
        }

        isEscaping = true;

        if (TrainingBattleManager.buildingDeck != null)
        {
            TrainingRunDeckPersistence.SaveRunDeckAsPermanentDeck(
                TrainingBattleManager.buildingDeck,
                SelectedButtonControl.selectedCharacterList,
                "이벤트 탈출로 저장덱을 갱신했습니다");
        }

        enhanceListView.Hide();
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.ResetRun();
        SceneManager.LoadScene(string.IsNullOrEmpty(escapeSceneName) ? "LobbyScene" : escapeSceneName);
    }

    private bool TryInitializeScene(out TrainingMapNodeData pendingNode)
    {
        pendingNode = null;
        if (!TrainingRunState.IsRunActive || !TrainingRunState.PendingNodeId.HasValue)
        {
            return false;
        }

        if (!TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out pendingNode))
        {
            return false;
        }

        RestSceneMode mode = ResolveSceneMode(pendingNode.nodeType);
        if (mode == RestSceneMode.None)
        {
            Debug.LogWarning("[RestSceneController] 휴식 또는 이벤트 노드가 아닙니다");
            return false;
        }

        return true;
    }

    private RestSceneMode ResolveSceneMode(TrainingNodeType nodeType)
    {
        switch (nodeType)
        {
            case TrainingNodeType.Rest:
                return RestSceneMode.Rest;
            case TrainingNodeType.Event:
                return RestSceneMode.Event;
            default:
                return RestSceneMode.None;
        }
    }

    private void EnsureRunDeck()
    {
        if (TrainingBattleManager.buildingDeck != null)
        {
            return;
        }

        TrainingBattleManager.buildingDeck = TrainingRunDeckPersistence.CreateRunDeck(SelectedButtonControl.selectedCharacterList);
        Debug.Log("[RestSceneController] 사용덱이 비어 있어 저장덱 기준으로 다시 구성했습니다");
    }

    private void RefreshHpText()
    {
        if (TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            if (hpText != null)
            {
                hpText.text = $"HP {hp} / {maxHp}";
            }

            return;
        }

        if (hpText != null)
        {
            hpText.text = "HP ? / ?";
        }
    }

    private void RefreshEnhanceCandidates()
    {
        enhanceCandidates = RestDeckEnhanceService.GetEnhanceableCandidates(TrainingBattleManager.buildingDeck);
    }

    private void RefreshActionButtons()
    {
        bool isRest = sceneMode == RestSceneMode.Rest;
        bool isEvent = sceneMode == RestSceneMode.Event;

        if (healButton != null)
        {
            healButton.gameObject.SetActive(isRest);
            healButton.interactable = isRest && !actionResolved;
        }

        if (enhanceButton != null)
        {
            enhanceButton.gameObject.SetActive(isRest);
            enhanceButton.interactable = isRest && !actionResolved && enhanceCandidates.Count > 0;
        }

        if (escapeButton != null)
        {
            escapeButton.gameObject.SetActive(isEvent);
            escapeButton.interactable = isEvent;
            if (isFinalEventNode)
            {
                SetButtonLabel(escapeButton, "덱 저장");
            }
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isFinalEventNode);
            nextButton.interactable = !isFinalEventNode && (isEvent || actionResolved);
            SetButtonLabel(nextButton, isEvent ? "계속 진행" : "다음");
        }

        if (!isRest || actionResolved)
        {
            enhanceListView.Hide();
        }
    }

    private void CompleteRestAction()
    {
        actionResolved = true;
        RefreshEnhanceCandidates();
        enhanceListView.Hide();
        RefreshActionButtons();
    }

    private void ApplySelectedEnhance(RestDeckEnhanceCandidate candidate, int enhanceCardId)
    {
        if (!RestDeckEnhanceService.TryApplyEnhance(TrainingBattleManager.buildingDeck, candidate, enhanceCardId))
        {
            RefreshEnhanceCandidates();
            RefreshActionButtons();
            if (enhanceCandidates.Count > 0)
            {
                enhanceListView.Show(enhanceCandidates, ApplySelectedEnhance);
            }

            return;
        }

        CompleteRestAction();
    }

    private void ReturnToMap()
    {
        if (!string.IsNullOrEmpty(TrainingRunState.MapSceneName))
        {
            SceneManager.LoadScene(TrainingRunState.MapSceneName);
        }
    }

    private void BindButtons()
    {
        if (healButton != null)
        {
            healButton.onClick.RemoveListener(OnClickHeal);
            healButton.onClick.AddListener(OnClickHeal);
        }

        if (enhanceButton != null)
        {
            enhanceButton.onClick.RemoveListener(OnClickEnhance);
            enhanceButton.onClick.AddListener(OnClickEnhance);
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
