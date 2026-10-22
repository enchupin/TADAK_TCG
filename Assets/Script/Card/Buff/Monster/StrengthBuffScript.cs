using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class StrengthBuffScript : MonsterBuffScript
{
    public override int BuffId => StrengthBuffId;

    // 음수 힘은 몬스터 피해를 낮춤
    public override bool IsActive(Monster monster, int stack) => stack != 0;

    public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
    {
        return currentBonus + stack;
    }
}
}
