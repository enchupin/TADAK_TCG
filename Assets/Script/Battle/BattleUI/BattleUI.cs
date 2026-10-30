using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using static BattleRuntimeDefinitions;

/// <summary>
/// 전투 UI 관리
/// HP, 에너지, 방어력, 덱 수를 표시
/// PlayerData.Instance / Monster를 직접 참조
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("플레이어 UI")]
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private Slider playerHPSlider;
    [SerializeField] private Image playerHPFillImage;
    [SerializeField] private Color barrierHPFillColor = new Color32(135, 206, 235, 255);
    // 파티 체력 바 중앙 수치와 좌측 보호막 표시 (상단 바의 playerHPText는 그대로 유지)
    [SerializeField] private HealthBarView playerHealthBarView;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private TextMeshProUGUI drawPileCountText;
    [SerializeField] private TextMeshProUGUI discardPileCountText;
    [SerializeField] private TextMeshProUGUI handCountText;
    
    [Header("아이덴티티 게이지 UI")]
    [SerializeField] private Slider firstIdentityGaugeSlider;
    [SerializeField] private Slider secondIdentityGaugeSlider;
    [SerializeField] private Slider thirdIdentityGaugeSlider;
    [SerializeField] private Image firstIdentityImage;
    [SerializeField] private Image secondIdentityImage;
    [SerializeField] private Image thirdIdentityImage;

    [Header("아이덴티티 충전 완료 이미지")]
    public Image firstIdentityFullImage;
    public Image secondIdentityFullImage;
    public Image thirdIdentityFullImage;

    [Header("데이터 참조")]
    [SerializeField] private TrainingBattleManager battleManager;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [Tooltip("설정 버튼 (비워 두면 OnClick에 OnClickSettingsButton이 연결된 버튼을 찾음)")]
    [SerializeField] private Button settingsButton;
    private Color defaultHPFillColor = Color.white;
    private bool hasDefaultHPFillColor;

    // 설정 버튼은 열기 · 닫기 전환
    public void OnClickSettingsButton()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ToggleSettingPanel();
    }

    private void Awake()
    {
        CacheHPFillDefaultColor();
        if (settingsButton == null)
        {
            settingsButton = UIButtonLookup.FindByOnClick(this, nameof(OnClickSettingsButton));
        }
        SettingsManager.RegisterSettingsButton(settingsButton);
        // 패배 패널: 필수 패널 (ESC 무시, 홈 버튼으로만 진행), 자체 배경이 전체 화면을 덮어 차단막 없음
        UIModalPanel.Ensure(defeatPanel, new UIModalPanel.Options {
            kind = UIModalKind.Required,
            useDimmer = false
        });
        HideDefeatPanel();
        UpdateIdentityGauges();
    }


    /// <summary>
    /// 플레이어 HP 업데이트
    /// </summary>
    public void UpdatePlayerHP()
    {
        PlayerData playerData = PlayerData.Instance;
        if (playerData == null) return;

        if (playerHPText != null)
            playerHPText.text = $"{playerData.hp}/{playerData.maxHP}";

        if (playerHPSlider != null)
        {
            int maxHp = Mathf.Max(1, playerData.maxHP);
            playerHPSlider.minValue = 0f;
            playerHPSlider.maxValue = maxHp;
            playerHPSlider.value = Mathf.Clamp(playerData.hp, 0, maxHp);
        }

        UpdatePlayerHPFillColor(playerData);
        playerHealthBarView?.Refresh(playerData.hp, playerData.maxHP, playerData.defense);
    }

    // 전투 밖(지도 · 휴식)에서 상단 바 체력을 런 체력으로 표시
    public void UpdateRunHealth(int hp, int maxHp)
    {
        if (playerHPText != null)
            playerHPText.text = $"{hp}/{maxHp}";
    }

    /// <summary>
    /// 에너지 업데이트
    /// </summary>
    public void UpdateEnergy()
    {
        if (PlayerData.Instance == null) return;
        if (playerEnergyText != null)
            playerEnergyText.text = $"{PlayerData.Instance.energy}/{PlayerData.Instance.maxEnergy}";
    }

    /// <summary>
    /// 플레이어 방어력 업데이트
    /// </summary>
    public void UpdatePlayerDefense()
    {
        PlayerData playerData = PlayerData.Instance;
        if (playerData == null) return;
        // 보호막 글자 표시 대신 체력 바의 방패 표시와 Fill 색으로 갱신
        UpdatePlayerHPFillColor(playerData);
        playerHealthBarView?.Refresh(playerData.hp, playerData.maxHP, playerData.defense);
    }



    public void UpdateDeckPileCount()
    {
        if (battleManager == null || battleManager.usableDeckManager == null) {
            return;
        }

        // 더미 아이콘 위에 숫자만 표시
        if (drawPileCountText != null) {
            drawPileCountText.text = battleManager.usableDeckManager.GetRemainingCardCount().ToString();
        }

        if (discardPileCountText != null) {
            discardPileCountText.text = battleManager.usableDeckManager.GetDiscardPileCount().ToString();
        }

        if (handCountText != null && battleManager.handManager != null) {
            handCountText.text = GameText.Ui("ui.combat.hand", new Dictionary<string, object> { { "count", battleManager.handManager.GetHandCount() } });
        }
    }
    
    public void UpdateIdentityGauges()
    {
        UpdateIdentityGaugeSlider(firstIdentityGaugeSlider, firstIdentityImage, firstIdentityFullImage, 0);
        UpdateIdentityGaugeSlider(secondIdentityGaugeSlider, secondIdentityImage, secondIdentityFullImage, 1);
        UpdateIdentityGaugeSlider(thirdIdentityGaugeSlider, thirdIdentityImage, thirdIdentityFullImage, 2);
    }

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI()
    {
        UpdatePlayerHP();
        UpdateEnergy();
        UpdatePlayerDefense();
        UpdateDeckPileCount();
        UpdateIdentityGauges();
    }

    public void ShowDefeatPanel()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            return;
        }

        OnClickReturnToLobbyAfterDefeat();
    }

    public void OnClickReturnToLobbyAfterDefeat()
    {
        HideDefeatPanel();
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.ResetRun();
        SceneManager.LoadScene(string.IsNullOrEmpty(lobbySceneName) ? "LobbyScene" : lobbySceneName);
    }

    private void CacheHPFillDefaultColor()
    {
        if (playerHPFillImage == null)
        {
            hasDefaultHPFillColor = false;
            return;
        }

        defaultHPFillColor = playerHPFillImage.color;
        hasDefaultHPFillColor = true;
    }

    private void UpdatePlayerHPFillColor(PlayerData playerData)
    {
        if (playerHPFillImage == null || playerData == null)
        {
            return;
        }

        if (!hasDefaultHPFillColor)
        {
            CacheHPFillDefaultColor();
        }

        playerHPFillImage.color = playerData.defense > 0
            ? barrierHPFillColor
            : defaultHPFillColor;
    }

    private void HideDefeatPanel()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }
    
    private void UpdateIdentityGaugeSlider(Slider targetSlider, Image identityImage, Image fullGaugeImage, int slotIndex)
    {
        if (targetSlider != null)
        {
            targetSlider.interactable = false;
            targetSlider.wholeNumbers = false;
            targetSlider.minValue = 0f;
            targetSlider.maxValue = 1f;
        }

        if (battleManager == null || !battleManager.TryGetSelectedCharacterBySlot(slotIndex, out Character character))
        {
            if (targetSlider != null)
            {
                targetSlider.SetValueWithoutNotify(1f);
            }
            if (identityImage != null)
            {
                identityImage.sprite = null;
                identityImage.enabled = false;
            }
            if (fullGaugeImage != null)
            {
                fullGaugeImage.gameObject.SetActive(false);
            }
            return;
        }

        if (identityImage != null)
        {
            identityImage.sprite = CharacterManager.GetCharacterByEnum(character)?.IdentitySprite;
            identityImage.enabled = identityImage.sprite != null;
        }

        int maxGauge = Mathf.Max(1, battleManager.GetIdentityCost(character));
        int currentGauge = Mathf.Clamp(battleManager.GetIdentityGauge(character), 0, maxGauge);

        // 충전될수록 검은색 필터가 사라지도록 남은 비율을 표시
        if (targetSlider != null)
        {
            targetSlider.SetValueWithoutNotify(1f - (float)currentGauge / maxGauge);
        }
        if (fullGaugeImage != null)
        {
            fullGaugeImage.gameObject.SetActive(currentGauge >= maxGauge);
        }
    }

}
