using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class EvadeBuffScript : PlayerBuffScript
{
    public override int BuffId => EvadeBuffId;

    public override bool TryPreventIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int incomingDamage, int stack)
    {
        if (player == null || attacker == null || incomingDamage <= 0 || stack <= 0)
        {
            return false;
        }

        player.ConsumeBuffStack(BuffId, 1);
        Debug.Log("회피가 발동해 공격을 피했습니다");
        battleManager?.HandlePlayerHit(attacker, 0, 0);
        return true;
    }
}
