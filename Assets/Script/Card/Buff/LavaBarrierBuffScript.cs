using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class LavaBarrierBuffScript : PlayerBuffScript
{
    public override int BuffId => LavaBarrierBuffId;

    public override bool TryPreventIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int incomingDamage, int stack)
    {
        if (player == null || incomingDamage <= 0 || stack <= 0)
        {
            return false;
        }

        player.ConsumeBuffStack(BuffId, 1);
        Debug.Log("용암 보호막이 발동해 피해를 받지 않았습니다");
        if (attacker != null)
        {
            battleManager?.HandlePlayerHit(attacker, 0, 0);
        }

        return true;
    }
}
