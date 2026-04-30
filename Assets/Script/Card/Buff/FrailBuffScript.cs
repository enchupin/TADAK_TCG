using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class FrailBuffScript : PlayerBuffScript
{
    public override int BuffId => FrailBuffId;

    public override int ModifyBarrierGain(TrainingBattleManager battleManager, PlayerData player, int stack, int currentGain)
    {
        if (stack <= 0)
        {
            return currentGain;
        }

        return Mathf.FloorToInt(currentGain * 0.5f);
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
