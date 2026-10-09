public sealed class ComboBuffScript : PlayerBuffScript
{
    private const int RequiredStack = 7;

    public override int BuffId => CreamBuffRuntimeUtility.ComboBuffId;

    public override float GetCalculatedCardBaseMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack < RequiredStack)
        {
            return currentMultiplier;
        }

        return currentMultiplier * 2f;
    }

    public override void OnPlayerAttackStarted(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player != null && stack >= RequiredStack)
        {
            player.RemoveBuffStack(BuffId);
        }
    }

    public override void OnPlayerDamageDealt(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int dealtDamage, int stack)
    {
        if (player == null || stack <= 0 || dealtDamage <= 0)
        {
            return;
        }

        player.AddBuff(BuffId, 1);
    }
}
