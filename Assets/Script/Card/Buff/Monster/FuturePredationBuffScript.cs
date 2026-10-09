using static BattleRuntimeDefinitions;
using System.Collections.Generic;

namespace MonsterBuffs
{
public sealed class FuturePredationBuffScript : MonsterBuffScript
{
    public override int BuffId => FuturePredationBuffId;
    private readonly Dictionary<Monster, int> activationCounts = new();

    public override int GetActivationCount(Monster monster)
        => monster != null && activationCounts.TryGetValue(monster, out int count) ? count : 0;

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null) activationCounts.Remove(monster);
    }

    public override void OnMonsterLeaveCombat(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null) activationCounts.Remove(monster);
    }

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        if (BuffCombatUtility.RemoveRandomBeneficialBuff(monster, BuffId) == null) return;
        monster.Heal(20);
        monster.AddBuff(StrengthBuffId, 2);
        activationCounts[monster] = GetActivationCount(monster) + 1;
    }
}
}
