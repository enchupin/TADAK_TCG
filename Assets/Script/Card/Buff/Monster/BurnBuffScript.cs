using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class BurnBuffScript : MonsterBuffScript
{
    public override int BuffId => BurnBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        int damage = battleManager != null ? battleManager.ResolvePlayerEffectDamage(stack) : stack;
        if (damage > 0)
        {
            monster.TakeDamage(damage, 0);
        }
    }
}
}
