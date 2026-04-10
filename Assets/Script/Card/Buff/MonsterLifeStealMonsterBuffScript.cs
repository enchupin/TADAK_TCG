using static BattleRuntimeDefinitions;

public sealed class MonsterLifeStealMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => MonsterLifeStealBuffId;

    public override void OnMonsterAttackResolved(TrainingBattleManager battleManager, Monster monster, PlayerData target, int attemptedDamage, int hpDamage, int stack)
    {
        if (monster == null || target == null || hpDamage <= 0 || stack <= 0)
        {
            return;
        }

        BuffCombatUtility.TransferPlayerMaxHpToMonster(target, monster, stack);
    }
}
