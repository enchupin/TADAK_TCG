using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class ThornBuffScript : MonsterBuffScript
{
    public override int BuffId => ThornBuffId;

    public override void OnAttackedByPlayer(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (battleManager?.playerData == null || stack <= 0)
        {
            return;
        }

        battleManager.playerData.TakeDamage(stack);
    }
}
}
