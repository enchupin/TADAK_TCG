using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 UI 관리
/// HP, 에너지, 방어력 등 표시
/// PlayerData.Instance / Monster를 직접 참조
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("플레이어 UI")]
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private TextMeshProUGUI playerDefenseText;

    [Header("데이터 참조")]
    [SerializeField] private Monster monster;
    [SerializeField] private TrainingBattleManager battleManager;

    /// <summary>
    /// 플레이어 HP 업데이트
    /// </summary>
    public void UpdatePlayerHP()
    {
        if (PlayerData.Instance == null) return;
        if (playerHPText != null)
            playerHPText.text = $"HP : {PlayerData.Instance.hp}/{PlayerData.Instance.maxHP}";
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


    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI()
    {
        UpdatePlayerHP();
        UpdateEnergy();
        UpdatePlayerDefense();
    }
}
