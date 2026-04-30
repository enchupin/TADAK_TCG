using static BattleRuntimeDefinitions;

public sealed class StrengthMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => StrengthBuffId;

    public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
    {
        return currentBonus + stack;
    }
}
