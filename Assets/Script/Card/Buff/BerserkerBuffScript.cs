public sealed class BerserkerBuffScript : PlayerBuffScript
{
    public override int BuffId => BattleRuntimeDefinitions.BerserkerBuffId;

    public override void OnPlayerHpLost(TrainingBattleManager battleManager, PlayerData player, int hpLoss, int stack)
    {
        if (battleManager == null || player == null || hpLoss <= 0 || stack <= 0 || !IsPlayerTurnState(battleManager))
        {
            return;
        }

        int resolvedDamage = battleManager.ResolvePlayerEffectDamage(stack);
        if (resolvedDamage <= 0)
        {
            return;
        }

        int totalDamageDealt = 0;
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            int dealtDamage = monster.TakeDamage(resolvedDamage, 0);
            totalDamageDealt += dealtDamage;
            battleManager.HandlePlayerDamageDealt(monster, dealtDamage);
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);
    }

    private static bool IsPlayerTurnState(TrainingBattleManager battleManager)
    {
        return battleManager.CurrentTurnState == BattleTurnState.PlayerTurnStart
            || battleManager.CurrentTurnState == BattleTurnState.PlayerAction
            || battleManager.CurrentTurnState == BattleTurnState.PlayerTurnEnd;
    }
}
