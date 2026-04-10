using static BattleRuntimeDefinitions;

public sealed class OverheatBuffScript : PlayerBuffScript
{
    public override int BuffId => OverheatBuffId;

    public override float GetCalculatedCardBaseMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        return currentMultiplier + (stack * 0.1f);
    }
}
