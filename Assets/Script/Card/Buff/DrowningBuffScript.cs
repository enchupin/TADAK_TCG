using static BattleRuntimeDefinitions;

public sealed class DrowningBuffScript : PlayerBuffScript
{
    public override int BuffId => DrowningBuffId;

    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0 || player.hp <= 0 || player.hp >= stack)
        {
            return;
        }

        player.LoseHp(player.hp);
        battleManager?.UpdateAllUI();
    }
}
