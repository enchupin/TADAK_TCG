using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class WuppiGuardBuffScript : PlayerBuffScript
{
    public override int BuffId => WuppiGuardBuffId;

    public override int GetRuntimeStack(TrainingBattleManager battleManager, PlayerData player)
    {
        return WuppiModeRuntimeUtility.IsMode(player, WuppiModeState.Guard) ? 1 : 0;
    }

    public override bool TryApplyToPlayer(TrainingBattleManager battleManager, PlayerData player, int amount)
    {
        WuppiModeRuntimeUtility.ChangeMode(battleManager, player, WuppiModeState.Guard);
        return true;
    }

    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        int runeStack = Mathf.Max(0, player.GetBuffStack(RuneBuffId));
        if (runeStack > 0)
        {
            player.AddDefense(runeStack);
        }
    }
}
