using UnityEngine;
using static BattleRuntimeDefinitions;

// 3072 성역: 다음 내 턴 시작 전까지 받는 공격 피해가 타격마다 {stack} 감소 (R-81)
public sealed class SanctuaryBuffScript : PlayerBuffScript
{
    public override int BuffId => SanctuaryBuffId;

    public override int ClampIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, int currentDamage)
    {
        if (attacker == null || stack <= 0)
        {
            return currentDamage;
        }

        return Mathf.Max(0, currentDamage - stack);
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }
}
