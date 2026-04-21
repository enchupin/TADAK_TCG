using static BattleRuntimeDefinitions;

public sealed class UnderwaterBreathingBuffScript : PlayerBuffScript
{
    public override int BuffId => UnderwaterBreathingBuffId;

    public override void OnEnemyDebuffApplied(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int crueltyStackBeforeApply)
    {
        if (player == null || buffId != DrowningBuffId || amount <= 0 || stack <= 0)
        {
            return;
        }

        player.AddDefense(stack);
    }
}
