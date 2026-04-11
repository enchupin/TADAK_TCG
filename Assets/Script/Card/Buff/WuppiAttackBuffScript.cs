using static BattleRuntimeDefinitions;

public sealed class WuppiAttackBuffScript : PlayerBuffScript
{
    public override int BuffId => WuppiAttackBuffId;

    public override int GetRuntimeStack(TrainingBattleManager battleManager, PlayerData player)
    {
        return WuppiModeRuntimeUtility.IsMode(player, WuppiModeState.Attack) ? 1 : 0;
    }

    public override bool TryApplyToPlayer(TrainingBattleManager battleManager, PlayerData player, int amount)
    {
        WuppiModeRuntimeUtility.ChangeMode(battleManager, player, WuppiModeState.Attack);
        return true;
    }

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        return stack > 0 ? currentMultiplier * 2f : currentMultiplier;
    }
}
