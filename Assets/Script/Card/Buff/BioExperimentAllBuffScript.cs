using static BattleRuntimeDefinitions;

public sealed class BioExperimentAllBuffScript : PlayerBuffScript
{
    public override int BuffId => BioExperimentAllBuffId;

    public override void OnEnemyDebuffApplied(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int crueltyStackBeforeApply)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            monster?.TakeDamage(stack, 0);
        }
    }
}
