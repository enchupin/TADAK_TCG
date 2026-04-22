using static BattleRuntimeDefinitions;

public sealed class DrowningOnDebuffBuffScript : PlayerBuffScript
{
    public override int BuffId => DrowningOnDebuffBuffId;

    public override void OnEnemyDebuffApplied(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int crueltyStackBeforeApply)
    {
        if (targetMonster == null || targetMonster.IsDead() || amount <= 0 || stack <= 0)
        {
            return;
        }

        targetMonster.AddBuff(DrowningBuffId, stack);
        battleManager?.UpdateAllUI();
    }
}
