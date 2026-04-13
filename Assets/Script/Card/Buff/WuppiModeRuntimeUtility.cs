using UnityEngine;
using static BattleRuntimeDefinitions;

public static class WuppiModeRuntimeUtility
{
    public static bool IsMode(PlayerData player, WuppiModeState mode)
    {
        return player != null && player.wuppiMode == mode;
    }

    public static void SyncModeBuffStacks(PlayerData player)
    {
        if (player == null)
        {
            return;
        }

        int runeStack = Mathf.Max(0, player.GetBuffStack(RuneBuffId));
        player.SetBuffStack(WuppiGuardBuffId, player.wuppiMode == WuppiModeState.Guard ? runeStack : 0);
        player.SetBuffStack(WuppiAttackBuffId, player.wuppiMode == WuppiModeState.Attack ? runeStack : 0);
    }

    public static void ChangeMode(TrainingBattleManager battleManager, PlayerData player, WuppiModeState newMode)
    {
        if (player == null)
        {
            return;
        }

        WuppiModeState previousMode = player.wuppiMode;
        if (previousMode == newMode)
        {
            SyncModeBuffStacks(player);
            return;
        }

        int runeStack = Mathf.Max(0, player.GetBuffStack(RuneBuffId));
        if (previousMode == WuppiModeState.Guard && runeStack > 0)
        {
            player.AddDefense(runeStack);
        }

        player.wuppiMode = newMode;
        SyncModeBuffStacks(player);

        int drawCount = Mathf.Max(0, player.GetBuffStack(ModeCycleBuffId));
        if (battleManager != null && drawCount > 0)
        {
            battleManager.DrawCards(drawCount);
        }
    }

    public static void HandleDirectBuffStackChange(TrainingBattleManager battleManager, PlayerData player, int changedBuffId)
    {
        if (player == null)
        {
            return;
        }

        if (changedBuffId == RuneBuffId)
        {
            SyncModeBuffStacks(player);
            return;
        }

        if (changedBuffId == WuppiGuardBuffId
            && player.wuppiMode == WuppiModeState.Guard
            && player.GetBuffStack(WuppiGuardBuffId) <= 0)
        {
            ChangeMode(battleManager, player, WuppiModeState.Normal);
            return;
        }

        if (changedBuffId == WuppiAttackBuffId
            && player.wuppiMode == WuppiModeState.Attack
            && player.GetBuffStack(WuppiAttackBuffId) <= 0)
        {
            ChangeMode(battleManager, player, WuppiModeState.Normal);
        }
    }
}
