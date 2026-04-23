using static BattleRuntimeDefinitions;

public sealed class TranceBuffScript : PlayerBuffScript
{
    public override int BuffId => TranceBuffId;

    public override void OnPlayerAttackResolved(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int barrierBefore, int barrierAfter, int stack)
    {
        if (battleManager == null || player == null || targetMonster == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(StrengthBuffId, stack);
        battleManager.ApplyBuffToPlayer(StrengthDecayBuffId, stack);
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.RemoveBuffStack(BuffId);
    }
}
