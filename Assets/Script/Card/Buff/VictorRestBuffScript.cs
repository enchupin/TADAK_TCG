public sealed class VictorRestBuffScript : PlayerBuffScript
{
    public override int BuffId => BattleRuntimeDefinitions.VictorRestBuffId;

    public override void OnBattleEnded(TrainingBattleManager battleManager, PlayerData player, int stack, bool isVictory)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.Heal(stack);
    }
}
