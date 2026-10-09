using UnityEngine;

namespace MonsterBuffs
{
public sealed class StrengthDecayBuffScript : MonsterBuffScript
{
    public override int BuffId => 4006;

    public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
    {
        if (stack <= 0)
        {
            return currentBonus;
        }

        return currentBonus - stack;
    }

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, stack);
    }
}
}
