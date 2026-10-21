using UnityEngine;

namespace MonsterBuffs
{
public sealed class StrengthDecayBuffScript : MonsterBuffScript
{
    public override int BuffId => 4006;

    public override bool IsActive(Monster monster, int stack) => stack != 0;

    // 음수 중첩이면 힘이 증가
    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack == 0)
        {
            return;
        }

        monster.AddBuff(BattleRuntimeDefinitions.StrengthBuffId, -stack);
        monster.RemoveBuffStack(BuffId);
    }
}
}
