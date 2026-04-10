using static BattleRuntimeDefinitions;

public sealed class CardUseAllEnemiesDamageBuffScript : PlayerBuffScript
{
    public override int BuffId => CardUseAllEnemiesDamageBuffId;

    public override int GetCardUseAllEnemiesDamage(TrainingBattleManager battleManager, PlayerData player, int stack, int currentDamage)
    {
        return currentDamage + stack;
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
