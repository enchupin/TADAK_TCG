using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class WeakBuffScript : PlayerBuffScript
{
    public override int BuffId => WeakBuffId;

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        float multiplier = BuffValueUtility.GetOutgoingDamageMultiplier(BuffId);
        return currentMultiplier * multiplier;
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}

public sealed class WeakMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => WeakBuffId;

    public override int ModifyOutgoingDamage(TrainingBattleManager battleManager, Monster monster, int stack, int currentDamage)
    {
        if (stack <= 0 || currentDamage <= 0)
        {
            return currentDamage;
        }

        float multiplier = BuffValueUtility.GetOutgoingDamageMultiplier(BuffId);
        return Mathf.Max(0, Mathf.FloorToInt(currentDamage * multiplier));
    }

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }
}
