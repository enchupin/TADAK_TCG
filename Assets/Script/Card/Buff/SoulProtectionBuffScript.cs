using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class SoulProtectionBuffScript : PlayerBuffScript
{
    private bool hasRevivedThisCombat;

    public override int BuffId => SoulProtectionBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        hasRevivedThisCombat = false;
    }

    public override bool TryConsumeFatalDamage(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || player.hp > 0 || stack <= 0 || hasRevivedThisCombat)
        {
            return false;
        }

        hasRevivedThisCombat = true;
        player.hp = 1;
        Debug.Log("영혼 보호가 발동해 체력을 1로 유지합니다");
        return true;
    }
}
