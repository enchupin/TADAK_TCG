using static BattleRuntimeDefinitions;

public sealed class CrueltyMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => CrueltyDebuffId;

    public override void OnEnemyDebuffApplied(TrainingBattleManager battleManager, Monster monster, int appliedBuffId, int amount, int stack, int crueltyStackBeforeApply)
    {
        if (monster == null || monster.IsDead() || amount <= 0 || stack <= 0)
        {
            return;
        }

        int damage = crueltyStackBeforeApply >= 0 ? crueltyStackBeforeApply : stack;
        if (damage > 0)
        {
            int resolvedDamage = battleManager != null ? battleManager.ResolvePlayerEffectDamage(damage) : damage;
            if (resolvedDamage > 0)
            {
                monster.TakeDamage(resolvedDamage, 0);
            }
        }
    }
}
