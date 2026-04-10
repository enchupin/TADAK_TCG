using static BattleRuntimeDefinitions;

public sealed class CorrosionEnhanceBuffScript : PlayerBuffScript
{
    public override int BuffId => CorrosionEnhanceBuffId;

    public override int ResolveAppliedMonsterBuffId(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int currentBuffId)
    {
        if (stack <= 0 || currentBuffId != CorrosionBuffId)
        {
            return currentBuffId;
        }

        return EnhancedCorrosionBuffId;
    }
}
