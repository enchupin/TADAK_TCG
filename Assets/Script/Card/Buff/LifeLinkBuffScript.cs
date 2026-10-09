using static BattleRuntimeDefinitions;

public sealed class LifeLinkBuffScript : PlayerBuffScript
{
    public override int BuffId => LifeLinkBuffId;

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (attacker == null || attacker.IsDead() || hpDamage <= 0 || stack <= 0) return;
        attacker.Heal(hpDamage);
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }

    public override void OnEnemyTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }
}
