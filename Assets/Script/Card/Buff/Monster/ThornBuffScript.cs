using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class ThornBuffScript : MonsterBuffScript
{
    public override int BuffId => ThornBuffId;

    public override void OnMonsterAfterTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int damageAfterDefense, int stack)
    {
        if (battleManager?.playerData == null || stack <= 0 || incomingDamage <= 0 || battleManager.CurrentTurnState != BattleTurnState.PlayerAction)
        {
            return;
        }

        battleManager.playerData.TakeDamage(stack);
    }
}
}
