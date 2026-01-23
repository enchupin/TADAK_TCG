using UnityEngine;
using UnityEngine.UI;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// 전투 UI 관리
/// HP, 에너지, 방어력 등 표시
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("플레이어 UI")]
    [SerializeField] private Slider playerHPSlider;
#if UNITY_TEXTMESHPRO
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private TextMeshProUGUI playerDefenseText;
#else
    [SerializeField] private Text playerHPText;
    [SerializeField] private Text playerEnergyText;
    [SerializeField] private Text playerDefenseText;
#endif
    
    [Header("적 UI")]
    [SerializeField] private Slider enemyHPSlider;
#if UNITY_TEXTMESHPRO
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI enemyDefenseText;
#else
    [SerializeField] private Text enemyHPText;
    [SerializeField] private Text enemyDefenseText;
#endif
    
    [Header("버튼")]
    [SerializeField] private Button endTurnButton;
    
    private BattleManager battleManager;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
        
        // 턴 종료 버튼 이벤트
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
    }
    
    /// <summary>
    /// 플레이어 HP 업데이트
    /// </summary>
    public void UpdatePlayerHP(int current, int max)
    {
        if (playerHPSlider != null)
        {
            playerHPSlider.maxValue = max;
            playerHPSlider.value = current;
        }
        
        if (playerHPText != null)
        {
            playerHPText.text = $"{current}/{max}";
        }
    }
    
    /// <summary>
    /// 적 HP 업데이트
    /// </summary>
    public void UpdateEnemyHP(int current, int max)
    {
        if (enemyHPSlider != null)
        {
            enemyHPSlider.maxValue = max;
            enemyHPSlider.value = current;
        }
        
        if (enemyHPText != null)
        {
            enemyHPText.text = $"{current}/{max}";
        }
    }
    
    /// <summary>
    /// 에너지 업데이트
    /// </summary>
    public void UpdateEnergy(int current, int max)
    {
        if (playerEnergyText != null)
        {
            string energyDisplay = "";
            for (int i = 0; i < max; i++)
            {
                energyDisplay += (i < current) ? "⚡" : "○";
            }
            playerEnergyText.text = energyDisplay + $" {current}/{max}";
        }
    }
    
    /// <summary>
    /// 플레이어 방어력 업데이트
    /// </summary>
    public void UpdatePlayerDefense(int defense)
    {
        if (playerDefenseText != null)
        {
            playerDefenseText.text = defense > 0 ? $"🛡 {defense}" : "";
        }
    }
    
    /// <summary>
    /// 적 방어력 업데이트
    /// </summary>
    public void UpdateEnemyDefense(int defense)
    {
        if (enemyDefenseText != null)
        {
            enemyDefenseText.text = defense > 0 ? $"🛡 {defense}" : "";
        }
    }
    
    /// <summary>
    /// 턴 종료 버튼 클릭
    /// </summary>
    private void OnEndTurnClicked()
    {
        if (battleManager != null)
        {
            battleManager.EndTurn();
        }
    }
    
    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI(PlayerData playerData, Monster monster)
    {
        UpdatePlayerHP(playerData.hp, playerData.maxHP);
        UpdateEnemyHP(monster.hp, monster.maxHP);
        UpdateEnergy(playerData.energy, playerData.maxEnergy);
        UpdatePlayerDefense(playerData.defense);
        UpdateEnemyDefense(monster.defense);
    }
}
