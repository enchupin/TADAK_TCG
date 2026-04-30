using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private TextMeshProUGUI playerDefenseText;
    [SerializeField] private TextMeshProUGUI drawPileCountText;
    [SerializeField] private TextMeshProUGUI discardPileCountText;
    [SerializeField] private TextMeshProUGUI handCountText;
    
    [Header("아이덴티티 게이지 UI")]
    [SerializeField] private Slider firstIdentityGaugeSlider;
    [SerializeField] private Slider secondIdentityGaugeSlider;
    [SerializeField] private Slider thirdIdentityGaugeSlider;

    [Header("데이터 참조")]
    [SerializeField] private TrainingBattleManager battleManager;
    private Color defaultHPFillColor = Color.white;
    private bool hasDefaultHPFillColor;

    private void Awake()
    {
        CacheHPFillDefaultColor();
    }


    /// <summary>
    /// 플레이어 HP 업데이트
    /// </summary>
    public void UpdatePlayerHP()
    {
        PlayerData playerData = PlayerData.Instance;
        if (playerData == null) return;

        if (playerHPText != null)
            playerHPText.text = $"HP : {playerData.hp}/{playerData.maxHP}";

        if (playerHPSlider != null)
        {
            int maxHp = Mathf.Max(1, playerData.maxHP);
            playerHPSlider.minValue = 0f;
            playerHPSlider.maxValue = maxHp;
            playerHPSlider.value = Mathf.Clamp(playerData.hp, 0, maxHp);
        }

        UpdatePlayerHPFillColor(playerData);
    }

    /// <summary>
    /// 에너지 업데이트
    /// </summary>
    public void UpdateEnergy()
    {
        if (PlayerData.Instance == null) return;
        if (playerEnergyText != null)
            playerEnergyText.text = $"Energy : {PlayerData.Instance.energy}/{PlayerData.Instance.maxEnergy}";
    }

    /// <summary>
    /// 플레이어 방어력 업데이트
    /// </summary>
    public void UpdatePlayerDefense()
    {
        if (PlayerData.Instance == null) return;
        if (playerDefenseText != null)
            playerDefenseText.text = $"Defense : {PlayerData.Instance.defense}";
    }



    public void UpdateDeckPileCount()
    {
        if (battleManager == null || battleManager.usableDeckManager == null) {
            return;
        }

        if (drawPileCountText != null) {
            drawPileCountText.text = $"Deck : {battleManager.usableDeckManager.GetRemainingCardCount()}";
        }

        if (discardPileCountText != null) {
            discardPileCountText.text = $"Discard Pile : {battleManager.usableDeckManager.GetDiscardPileCount()}";
        }

        if (handCountText != null && battleManager.handManager != null) {
            handCountText.text = $"Hand : {battleManager.handManager.GetHandCount()}";
        }
    }
    
    public void UpdateIdentityGauges()
    {
        UpdateIdentityGaugeSlider(firstIdentityGaugeSlider, 0);
        UpdateIdentityGaugeSlider(secondIdentityGaugeSlider, 1);
        UpdateIdentityGaugeSlider(thirdIdentityGaugeSlider, 2);
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
    
    private void UpdateIdentityGaugeSlider(Slider targetSlider, int slotIndex)
    {
        if (targetSlider == null)
        {
            return;
        }

        if (battleManager == null ||
            SelectedButtonControl.selectedCharacterList == null ||
            slotIndex < 0 ||
            slotIndex >= SelectedButtonControl.selectedCharacterList.Count)
        {
            targetSlider.minValue = 0f;
            targetSlider.maxValue = 1f;
            targetSlider.SetValueWithoutNotify(0f);
            return;
        }

        Character character = SelectedButtonControl.selectedCharacterList[slotIndex];
        int maxGauge = Mathf.Max(1, battleManager.GetIdentityCost(character));
        int currentGauge = Mathf.Clamp(battleManager.GetIdentityGauge(character), 0, maxGauge);

        targetSlider.minValue = 0f;
        targetSlider.maxValue = maxGauge;
        targetSlider.SetValueWithoutNotify(currentGauge);
    }

}
