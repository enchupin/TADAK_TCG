using UnityEngine;

/// <summary>
/// 에너지 효과
/// 에너지를 추가합니다.
/// </summary>
[System.Serializable]
public class EnergyEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = EffectAmountResolver.Resolve(amount, amountFormula, battleManager.battleContext, battleManager.playerData);
        battleManager.playerData.AddEnergy(finalAmount);
        battleManager.UpdateAllUI();
    }
}
