using UnityEngine;

public class TriggerEffect : ICardEffect
{
    public string timing;
    public TargetType target = TargetType.None;
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 1);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null || string.IsNullOrWhiteSpace(timing))
        {
            return;
        }

        int repeatCount = ResolveAmount(battleManager, forwardedAmount);
        if (string.Equals(timing, "OnTurnEnd", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.AddTurnEndTriggerRepeat(repeatCount);
            return;
        }

        if (string.Equals(timing, "Feather", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.TriggerFeather(target, repeatCount);
            return;
        }

        if (string.Equals(timing, "FeatherUntilEmpty", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.TriggerFeatherUntilEmpty(target);
            return;
        }

        if (string.Equals(timing, "ReplayExhaustedFeather", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.StartCoroutine(CardEffectSequence.Run(battleManager.ReplayExhaustedFeathersSequence(), battleManager));
            return;
        }

        if (string.Equals(timing, "Freeze", System.StringComparison.OrdinalIgnoreCase))
        {
            foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
            {
                TriggerFreezeImmediately(battleManager, monster);
            }
            battleManager.UpdateAllUI();
            return;
        }

        if (string.Equals(timing, "Stun", System.StringComparison.OrdinalIgnoreCase))
        {
            foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
            {
                if (monster != null && !monster.IsDead())
                {
                    monster.SetStunIntent();
                }
            }
            battleManager.UpdateAllUI();
            return;
        }

        Debug.LogWarning($"[TriggerEffect] 아직 지원하지 않는 timing입니다: {timing}");
    }

    // 빙결 중첩과 무관하게 즉시 발동: 빙결을 모두 잃고 기절 (보스는 대신 피해 20)
    private static void TriggerFreezeImmediately(TrainingBattleManager battleManager, Monster monster)
    {
        if (monster == null || monster.IsDead() || monster.GetBuffStack(BattleRuntimeDefinitions.FreezeBuffId) <= 0)
        {
            return;
        }

        monster.RemoveBuffStack(BattleRuntimeDefinitions.FreezeBuffId);
        if (monster.IsBoss)
        {
            int damage = battleManager.ResolvePlayerEffectDamage(20);
            if (damage > 0)
            {
                monster.TakeDamage(damage, 0);
            }
            return;
        }

        monster.SetStunIntent();
    }

    public System.Collections.IEnumerator ExecuteSequence(TrainingBattleManager battleManager, int? amount = null)
    {
        if (battleManager == null) yield break;
        if (string.Equals(timing, "ReplayExhaustedFeather", System.StringComparison.OrdinalIgnoreCase))
            yield return battleManager.ReplayExhaustedFeathersSequence();
        else
            Execute(battleManager, amount ?? 1);
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(1, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return forwardedAmount > 0 ? forwardedAmount : 1;
    }
}
