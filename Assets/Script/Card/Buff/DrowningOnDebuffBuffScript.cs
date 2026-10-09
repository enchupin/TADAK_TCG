using static BattleRuntimeDefinitions;

public sealed class DrowningOnDebuffBuffScript : PlayerBuffScript
{
    private bool isApplyingDrowning;

    public override int BuffId => DrowningOnDebuffBuffId;

    public override void OnEnemyDebuffApplied(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int crueltyStackBeforeApply)
    {
        if (targetMonster == null || targetMonster.IsDead() || amount <= 0 || stack <= 0 || isApplyingDrowning)
        {
            return;
        }

        isApplyingDrowning = true;
        try
        {
            battleManager?.ApplyBuffToMonster(targetMonster, DrowningBuffId, stack);
        }
        finally
        {
            isApplyingDrowning = false;
        }
        battleManager?.UpdateAllUI();
    }
}
