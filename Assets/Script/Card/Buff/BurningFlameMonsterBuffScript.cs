using static BattleRuntimeDefinitions;

public sealed class BurningFlameMonsterBuffScript : MonsterBuffScript
{
    private const int ReviveDelayTurns = 2;

    public override int BuffId => BurningFlameBuffId;

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (battleManager == null || monster == null || stack <= 0 || !HasOtherLivingSameMonster(battleManager, monster))
        {
            return;
        }

        battleManager.ScheduleMonsterRevive(monster, ReviveDelayTurns);
    }

    public override bool CanRevive(TrainingBattleManager battleManager, Monster monster, int stack, bool currentCanRevive)
    {
        return currentCanRevive && stack > 0 && HasOtherLivingSameMonster(battleManager, monster);
    }

    private static bool HasOtherLivingSameMonster(TrainingBattleManager battleManager, Monster monster)
    {
        if (battleManager == null || monster == null)
        {
            return false;
        }

        foreach (Monster livingMonster in battleManager.GetLivingMonsters())
        {
            if (livingMonster == null || livingMonster == monster || livingMonster.IsDead())
            {
                continue;
            }

            if (livingMonster.MonsterId == monster.MonsterId)
            {
                return true;
            }
        }

        return false;
    }
}
