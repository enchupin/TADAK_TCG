using static BattleRuntimeDefinitions;

public sealed class FuturePredationMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => FuturePredationBuffId;

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        BuffCombatUtility.RemoveRandomBeneficialBuff(monster);
        monster.Heal(50, true);
        monster.AddBuff(StrengthBuffId, 2);
    }
}
