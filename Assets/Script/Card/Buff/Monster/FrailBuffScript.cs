using UnityEngine;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class FrailBuffScript : MonsterBuffScript
{
    public override int BuffId => FrailBuffId;

    public override int ModifyBarrierGain(TrainingBattleManager battleManager, Monster monster, int stack, int currentGain)
    {
        if (stack <= 0)
        {
            return currentGain;
        }

        return Mathf.FloorToInt(currentGain * 0.5f);
    }

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }
}
}
