using System.Collections.Generic;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class VoidShellBuffScript : MonsterBuffScript
{
    // 몬스터별로 발동한 턴 번호 (내 턴과 몬스터 턴은 서로 다른 번호)
    private readonly Dictionary<Monster, int> triggeredTurnSequence = new();

    public override int BuffId => VoidShellBuffId;

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredTurnSequence.Remove(monster);
        }
    }

    public override void OnMonsterRevived(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredTurnSequence.Remove(monster);
        }
    }

    public override void OnMonsterLeaveCombat(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredTurnSequence.Remove(monster);
        }
    }

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            triggeredTurnSequence.Remove(monster);
        }
    }

    public override void OnMonsterBeforeTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int stack)
    {
        if (monster == null || incomingDamage <= 0 || stack <= 0 || monster.IsDead())
        {
            return;
        }

        int turnSequence = (battleManager ?? TrainingBattleManager.Instance)?.TurnSequence ?? 0;
        if (triggeredTurnSequence.TryGetValue(monster, out int triggeredSequence) && triggeredSequence == turnSequence)
        {
            return;
        }

        triggeredTurnSequence[monster] = turnSequence;

        monster.AddDefense(stack);
    }
}
}
