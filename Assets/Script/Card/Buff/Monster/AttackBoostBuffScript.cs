using System.Collections.Generic;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
    public sealed class AttackBoostBuffScript : MonsterBuffScript
    {
        public override int BuffId => AttackBoostBuffId;
        private readonly Dictionary<Monster, int> stacksAtAttackStart = new();

        public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
            => currentBonus + stack;

        public override void OnAttackActionStarted(Monster monster, int stack)
        {
            if (monster != null) stacksAtAttackStart[monster] = stack;
        }

        // 다단 공격에서도 첫 타격 1회에만 더하고 공격 시작 시점의 중첩을 소모 (E-10)
        public override void OnMonsterAttackResolved(TrainingBattleManager battleManager, Monster monster, PlayerData target, int attemptedDamage, int hpDamage, int stack)
        {
            if (monster == null || !stacksAtAttackStart.TryGetValue(monster, out int startStack)) return;
            monster.ConsumeBuffStack(BuffId, startStack);
            stacksAtAttackStart.Remove(monster);
        }

        public override void OnAttackActionEnded(Monster monster)
        {
            if (monster == null || !stacksAtAttackStart.TryGetValue(monster, out int stack)) return;
            // 연타 도중에는 유지하고 한 공격 행동이 끝난 뒤 시작 시점의 중첩만 소모
            monster.ConsumeBuffStack(BuffId, stack);
            stacksAtAttackStart.Remove(monster);
        }
    }
}
