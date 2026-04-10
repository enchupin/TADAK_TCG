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

        int bondLoss = System.Math.Min(stack, player.GetBuffStack(GlacierBondBuffId));
        if (bondLoss <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(GlacierBondBuffId, bondLoss);
        battleManager.ApplyBuffToPlayer(StrengthBuffId, bondLoss);
    }
}
