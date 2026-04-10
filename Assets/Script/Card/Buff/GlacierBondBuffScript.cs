using static BattleRuntimeDefinitions;

public sealed class GlacierBondBuffScript : PlayerBuffScript
{
    public override int BuffId => GlacierBondBuffId;

    public override int GetAdditionalBarrierGain(TrainingBattleManager battleManager, PlayerData player, int stack, int currentGain)
    {
        return currentGain + stack;
    }
}
