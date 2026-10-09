using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class PoisonousMushroomBuffScript : MonsterBuffScript
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
}
