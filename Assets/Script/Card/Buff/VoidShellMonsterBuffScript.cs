using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class VoidShellMonsterBuffScript : MonsterBuffScript
{
    private readonly HashSet<Monster> triggeredMonsters = new();

    public override int BuffId => VoidShellBuffId;

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredMonsters.Remove(monster);
        }
    }

    public override void OnMonsterRevived(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredMonsters.Remove(monster);
        }
    }

    public override void OnMonsterLeaveCombat(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredMonsters.Remove(monster);
        }
    }

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredMonsters.Remove(monster);
        }
    }

    public override void OnMonsterBeforeTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int stack)
    {
        if (monster == null || incomingDamage <= 0 || stack <= 0 || monster.IsDead())
        {
            return;
        }

        if (!triggeredMonsters.Add(monster))
        {
            return;
        }

        monster.AddDefense(stack, false);
    }
}
