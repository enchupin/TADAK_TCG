using static BattleRuntimeDefinitions;

public sealed class DamageClampToOneBuffScript : PlayerBuffScript
{
    public override int BuffId => DamageClampToOneBuffId;

    public override int ClampIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, int currentDamage)
    {
        if (stack <= 0 || currentDamage <= 0)
        {
            return currentDamage;
        }

        return 1;
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
