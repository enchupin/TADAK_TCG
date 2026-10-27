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

    // 매 턴 첫 피해를 입은 뒤 보호막을 얻어 다음 피해부터 막음 (첫 피해로 사망하면 발동 안 함)
    public override void OnMonsterAfterTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int damageAfterDefense, int stack)
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
