using static BattleRuntimeDefinitions;

public sealed class GlacierBondMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => GlacierBondBuffId;

    public override int ModifyBarrierGain(TrainingBattleManager battleManager, Monster monster, int stack, int currentGain)
    {
        return currentGain + stack;
    }
}
