using UnityEngine;

namespace MonsterBuffs
{
public sealed class StrengthDecayBuffScript : MonsterBuffScript
{
    public override int BuffId => 4006;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BattleRuntimeDefinitions.StrengthBuffId, stack);
        monster.ConsumeBuffStack(BuffId, stack);
    }
}
}
