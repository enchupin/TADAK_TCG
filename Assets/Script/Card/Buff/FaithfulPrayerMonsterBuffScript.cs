using static BattleRuntimeDefinitions;

public sealed class FaithfulPrayerMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => FaithfulPrayerBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || monster.IsDead() || stack <= 0)
        {
            return;
        }

        monster.AddBuff(StrengthBuffId, stack);
        monster.AddBuff(GlacierBondBuffId, stack);
    }
}
