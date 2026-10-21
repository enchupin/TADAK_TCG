using UnityEngine;
using static BattleRuntimeDefinitions;

// 3070 동면: 보호막이 막아 낸 공격 피해를 누적하고 내 턴 종료 시 누적의 {stack}×10%를 회복한 뒤 비움 (R-80)
public sealed class HibernationBuffScript : PlayerBuffScript
{
    private const float HealRatioPerStack = 0.1f;

    private int blockedAttackDamage;

    public override int BuffId => HibernationBuffId;

    public int BlockedAttackDamage => blockedAttackDamage;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        blockedAttackDamage = 0;
    }

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (attacker == null || blockedDamage <= 0 || stack <= 0)
        {
            return;
        }

        blockedAttackDamage += blockedDamage;
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        int healAmount = Mathf.FloorToInt(blockedAttackDamage * stack * HealRatioPerStack);
        blockedAttackDamage = 0;
        if (healAmount > 0)
        {
            player.Heal(healAmount);
        }
    }
}
