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

        public override void OnAttackActionEnded(Monster monster)
        {
            if (monster == null || !stacksAtAttackStart.TryGetValue(monster, out int stack)) return;
            // 연타 도중에는 유지하고 한 공격 행동이 끝난 뒤 시작 시점의 중첩만 소모
            monster.ConsumeBuffStack(BuffId, stack);
            stacksAtAttackStart.Remove(monster);
        }
    }
}
