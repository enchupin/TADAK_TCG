using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 UI 관리
/// HP, 에너지, 방어력 등 표시
/// </summary>
public class BattleUI : MonoBehaviour {
    [Header("플레이어 UI")]
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private TextMeshProUGUI playerDefenseText;

    [Header("적 UI")]
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI enemyDefenseText;

    [Header("버튼")]
    [SerializeField] private Button endTurnButton;

    /*
    private void Start() {
        UpdateAllUI();
    }
    */
    /// <summary>
    /// 플레이어 HP 업데이트
    /// </summary>
    public void UpdatePlayerHP() {
        if (TrainingBattleManager.Instance == null) return;
        int current = TrainingBattleManager.Instance.playerData.hp;
        int max = TrainingBattleManager.Instance.playerData.maxHP;
        if (playerHPText != null) {
            playerHPText.text = $"{current}/{max}";
        }
    }

    /// <summary>
    /// 적 HP 업데이트
    /// </summary>
    public void UpdateEnemyHP() {
        if (TrainingBattleManager.Instance == null) return;
        int current = TrainingBattleManager.Instance.monster.hp;
        int max = TrainingBattleManager.Instance.monster.maxHP;
        if (enemyHPText != null) {
            enemyHPText.text = $"{current}/{max}";
        }
    }

    /// <summary>
    /// 에너지 업데이트
    /// </summary>
    public void UpdateEnergy() {
        if (TrainingBattleManager.Instance == null) return;
        int current = TrainingBattleManager.Instance.playerData.energy;
        int max = TrainingBattleManager.Instance.playerData.maxEnergy;
        if (playerEnergyText != null) {
            string energyDisplay = "";
            for (int i = 0; i < max; i++) {
                energyDisplay += (i < current) ? "⚡" : "○";
            }
            playerEnergyText.text = energyDisplay + $" {current}/{max}";
        }
    }

    /// <summary>
    /// 플레이어 방어력 업데이트
    /// </summary>
    public void UpdatePlayerDefense() {
        if (TrainingBattleManager.Instance == null) return;
        int defense = TrainingBattleManager.Instance.playerData.defense;
        if (playerDefenseText != null) {
            playerDefenseText.text = defense > 0 ? $"🛡 {defense}" : "";
        }
    }

    /// <summary>
    /// 적 방어력 업데이트
    /// </summary>
    public void UpdateEnemyDefense() {
        if (TrainingBattleManager.Instance == null) return;
        int defense = TrainingBattleManager.Instance.monster.defense;
        if (enemyDefenseText != null) {
            enemyDefenseText.text = defense > 0 ? $"🛡 {defense}" : "";
        }
    }


    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI() {
        UpdatePlayerHP();
        UpdateEnemyHP();
        UpdateEnergy();
        UpdatePlayerDefense();
        UpdateEnemyDefense();
    }
}
