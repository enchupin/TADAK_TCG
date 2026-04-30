using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class RootedMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => RootedBuffId;

    public override bool ShouldKeepBarrierOnTurnStart(TrainingBattleManager battleManager, Monster monster, int stack, bool currentShouldKeep)
    {
        return currentShouldKeep || stack > 0;
    }

    public override void OnDefenseChanged(TrainingBattleManager battleManager, Monster monster, int previousDefense, int currentDefense, int stack)
    {
        if (monster == null || stack <= 0 || previousDefense <= 0 || currentDefense > 0 || monster.hp <= 0)
        {
            return;
        }

        monster.RemoveBuffStack(BuffId);
        monster.SetStunIntent();
        Debug.Log($"[Monster] {monster.name} 뿌리내림을 잃고 기절합니다.");
    }
}
