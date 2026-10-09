using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
    public sealed class HarmonyBuffScript : MonsterBuffScript
    {
        public override int BuffId => HarmonyBuffId;

        public override void OnMonsterAttackResolved(TrainingBattleManager battleManager, Monster monster, PlayerData target, int attemptedDamage, int hpDamage, int stack)
        {
            if (monster != null && stack > 0 && hpDamage > 0)
                monster.AddPermanentDefense(hpDamage);
        }
    }
}
