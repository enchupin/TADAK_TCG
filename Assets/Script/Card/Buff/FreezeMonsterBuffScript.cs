using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class FreezeMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => FreezeBuffId;

    public override void OnBuffApplied(TrainingBattleManager battleManager, Monster monster, int appliedAmount, int stack)
    {
        ResolveThreshold(monster);
    }

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        ResolveThreshold(monster);
    }

    private static void ResolveThreshold(Monster monster)
    {
        if (monster == null)
        {
            return;
        }

        int freezeStack = monster.GetBuffStack(FreezeBuffId);
        while (freezeStack >= 7 && !monster.IsDead())
        {
            monster.ConsumeBuffStack(FreezeBuffId, 7);
            freezeStack -= 7;

            if (monster.IsBoss)
            {
                Debug.Log($"[Monster] {monster.name}은 빙결 7스택으로 대신 20 피해를 받습니다.");
                monster.TakeDamage(20, 0);
                continue;
            }

            monster.SkipCurrentTurnActionOnce();
            Debug.Log($"[Monster] {monster.name} 빙결 7스택으로 기절 상태가 되어 이번 턴 행동을 쉽니다.");
            break;
        }
    }
}
