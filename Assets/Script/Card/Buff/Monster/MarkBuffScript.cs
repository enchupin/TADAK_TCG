using UnityEngine;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
// 4018 표식: 공격 피해를 받으면 표식을 모두 소모하고 최대 체력의 {stack}%만큼 추가 피해 (v1.0 미사용)
public sealed class MarkBuffScript : MonsterBuffScript
{
    public override int BuffId => MarkBuffId;

    public override void OnAttackedByPlayer(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || monster.IsDead() || stack <= 0)
        {
            return;
        }

        monster.RemoveBuffStack(BuffId);
        int damage = Mathf.FloorToInt(monster.maxHP * stack / 100f);
        if (damage > 0)
        {
            monster.TakeDamage(battleManager != null ? battleManager.ResolvePlayerEffectDamage(damage) : damage, 0);
        }
    }
}
}
