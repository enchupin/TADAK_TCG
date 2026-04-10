using static BattleRuntimeDefinitions;

public sealed class StrengthMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => StrengthBuffId;

    public override int ModifyOutgoingDamage(TrainingBattleManager battleManager, Monster monster, int stack, int currentDamage)
    {
        return currentDamage <= 0 ? 0 : currentDamage + stack;
    }
}
