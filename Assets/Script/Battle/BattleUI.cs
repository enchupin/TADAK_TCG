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

    [Header("적 UI")]
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI enemyDefenseText;

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
    /// 적 HP 업데이트
    /// </summary>
    public void UpdateEnemyHP()
    {
        if (monster == null) return;
        if (enemyHPText != null)
            enemyHPText.text = $"HP : {monster.hp}/{monster.maxHP}";
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
    /// 적 방어력 업데이트
    /// </summary>
    public void UpdateEnemyDefense()
    {
        if (monster == null) return;
        if (enemyDefenseText != null)
            enemyDefenseText.text = monster.defense > 0 ? $"🛡 {monster.defense}" : "";
    }

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI()
    {
        UpdatePlayerHP();
        UpdateEnemyHP();
        UpdateEnergy();
        UpdatePlayerDefense();
        UpdateEnemyDefense();
    }
}
