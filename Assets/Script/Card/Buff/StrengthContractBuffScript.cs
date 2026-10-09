using static BattleRuntimeDefinitions;

public sealed class StrengthContractBuffScript : PlayerBuffScript
{
    public override int BuffId => StrengthContractBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || player == null || stack <= 0)
        {
            return;
        }

        int bondLoss = stack;
        if (player.GetBuffStack(GlacierBondBuffId) < bondLoss)
        {
            return;
        }

        player.ConsumeBuffStack(GlacierBondBuffId, bondLoss);
        battleManager.ApplyBuffToPlayer(StrengthBuffId, bondLoss);
    }
}
