using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class ParasiticMushroomBuffScript : MonsterBuffScript
{
    public override int BuffId => ParasiticMushroomBuffId;

    public override void OnMonsterAttackResolved(TrainingBattleManager battleManager, Monster monster, PlayerData target, int attemptedDamage, int hpDamage, int stack)
    {
        if (target == null || hpDamage <= 0 || stack <= 0)
        {
            return;
        }

        BuffCombatUtility.ReducePlayerMaxHp(target, UnityEngine.Mathf.Max(0, stack * 2));
    }
}
}
