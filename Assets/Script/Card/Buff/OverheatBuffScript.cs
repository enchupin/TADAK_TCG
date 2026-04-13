using static BattleRuntimeDefinitions;

public sealed class OverheatBuffScript : PlayerBuffScript
{
    public override int BuffId => OverheatBuffId;

    public override float GetCalculatedCardBaseMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        return currentMultiplier * (1f + (stack * 0.1f));
    }
}
