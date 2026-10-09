using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class StrengthBuffScript : MonsterBuffScript
{
    public override int BuffId => StrengthBuffId;

    public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
    {
        return currentBonus + stack;
    }
}
}
