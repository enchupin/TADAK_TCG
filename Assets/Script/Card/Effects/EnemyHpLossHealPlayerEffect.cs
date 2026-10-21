using UnityEngine;

// 대상 적이 이번 턴에 잃은 체력만큼 파티 회복 (102054)
[System.Serializable]
public class EnemyHpLossHealPlayerEffect : ICardEffect
{
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        Monster targetMonster = CardEffectRuntimeUtility.ResolveSingleEnemyTarget(battleManager);
        if (targetMonster == null)
        {
            Debug.LogWarning("[EnemyHpLossHealPlayerEffect] 대상 적을 찾지 못했습니다");
            return;
        }

        int healAmount = targetMonster.HpLostThisTurn;
        if (healAmount > 0)
        {
            battleManager.playerData?.Heal(healAmount);
        }

        battleManager.UpdateAllUI();
    }
}
