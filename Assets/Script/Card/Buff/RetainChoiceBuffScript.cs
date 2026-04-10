using static BattleRuntimeDefinitions;

public sealed class RetainChoiceBuffScript : PlayerBuffScript
{
    public override int BuffId => RetainChoiceBuffId;

    public override int GetTurnEndRetainCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentCount)
    {
        return currentCount + stack;
    }
}
