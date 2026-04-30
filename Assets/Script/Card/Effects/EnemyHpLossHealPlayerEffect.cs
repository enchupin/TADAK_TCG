using UnityEngine;

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
        if (targetMonster == null || targetMonster.IsDead())
        {
            Debug.LogWarning("[EnemyHpLossHealPlayerEffect] 대상 적을 찾지 못했습니다");
            return;
        }

        battleManager.RegisterMonsterHpLossHealPlayerThisTurn(targetMonster);
    }
}
