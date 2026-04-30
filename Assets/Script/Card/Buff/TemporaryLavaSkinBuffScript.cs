using static BattleRuntimeDefinitions;

public sealed class TemporaryLavaSkinBuffScript : PlayerBuffScript
{
    public override int BuffId => TemporaryLavaSkinBuffId;

    private int runtimeStack;

    public override int GetRuntimeStack(TrainingBattleManager battleManager, PlayerData player)
    {
        return runtimeStack;
    }

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        runtimeStack = 0;
    }

    public override bool TryApplyToPlayer(TrainingBattleManager battleManager, PlayerData player, int amount)
    {
        if (player == null || amount <= 0)
        {
            return false;
        }

        runtimeStack += amount;
        player.AddBuff(LavaSkinBuffId, amount);
        return true;
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(LavaSkinBuffId, stack);
        runtimeStack = 0;
    }
}
