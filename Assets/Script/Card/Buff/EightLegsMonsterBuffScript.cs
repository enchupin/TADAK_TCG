using System.Collections.Generic;
using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class EightLegsMonsterBuffScript : MonsterBuffScript
{
    private const int MaxLegCount = 8;
    private const int HpLossPerLeg = 50;

    private readonly Dictionary<Monster, int> accumulatedHpLossByMonster = new();

    public override int BuffId => EightLegsBuffId;

    public override void OnMonsterHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
        if (monster == null || hpLoss <= 0 || stack <= 0)
        {
            return;
        }

        accumulatedHpLossByMonster.TryGetValue(monster, out int accumulatedHpLoss);
        accumulatedHpLoss += hpLoss;
        accumulatedHpLossByMonster[monster] = accumulatedHpLoss;

        int targetLostLegCount = Mathf.Min(MaxLegCount, accumulatedHpLoss / HpLossPerLeg);
        int currentLegCount = Mathf.Max(0, monster.GetBuffStack(BuffId));
        int desiredLegCount = Mathf.Max(0, MaxLegCount - targetLostLegCount);
        int legLossCount = Mathf.Max(0, currentLegCount - desiredLegCount);
        if (legLossCount <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, legLossCount);
        monster.AddBuff(StrengthBuffId, legLossCount * 3);
    }

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        RemoveState(monster);
    }

    public override void OnMonsterLeaveCombat(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        RemoveState(monster);
    }

    private void RemoveState(Monster monster)
    {
        if (monster != null)
        {
            accumulatedHpLossByMonster.Remove(monster);
        }
    }
}
