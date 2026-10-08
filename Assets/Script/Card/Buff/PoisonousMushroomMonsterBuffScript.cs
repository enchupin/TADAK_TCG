using static BattleRuntimeDefinitions;

public sealed class PoisonousMushroomMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => PoisonousMushroomBuffId;

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (battleManager?.playerData == null || stack <= 0)
        {
            return;
        }

        battleManager.playerData.AddBuff(EnhancedCorrosionBuffId, stack * 2);
    }
}
