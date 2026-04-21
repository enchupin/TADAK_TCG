using static BattleRuntimeDefinitions;

public sealed class DrowningBuffScript : PlayerBuffScript
{
    public override int BuffId => DrowningBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0 || player.hp <= 0 || player.hp >= stack)
        {
            return;
        }

        player.LoseHp(player.hp);
        battleManager?.UpdateAllUI();
    }
}

public sealed class DrowningMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => DrowningBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0 || monster.IsDead() || monster.hp >= stack)
        {
            return;
        }

        monster.Kill();
        battleManager?.UpdateAllUI();
    }
}
