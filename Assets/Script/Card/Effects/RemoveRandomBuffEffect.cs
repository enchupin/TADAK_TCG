using System.Collections.Generic;
using UnityEngine;

// 무작위 효과 제거
// operation = Harmful: 대상의 해로운 효과 제거 (기본, 대상 Self = 파티)
// operation = Beneficial: 적의 이로운 효과 제거 (1xxx · 3xxx만, R-82)
// operation = Steal: 적의 이로운 효과를 제거하고 같은 중첩으로 파티가 얻음
public class RemoveRandomBuffEffect : ICardEffect
{
    public int amount = 1;
    public string operation;
    public TargetType target = TargetType.Self;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int count = Mathf.Max(1, amount);
        bool steal = string.Equals(operation, "Steal", System.StringComparison.OrdinalIgnoreCase);
        bool beneficial = steal || string.Equals(operation, "Beneficial", System.StringComparison.OrdinalIgnoreCase);

        if (target == TargetType.Self || target == TargetType.None)
        {
            RemoveFrom(battleManager.playerData?.currentBuffs, count, beneficial, null, battleManager);
            CreamBuffRuntimeUtility.SyncEnergyOverflow(battleManager.playerData);
        }
        else
        {
            foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
            {
                RemoveFrom(monster?.currentBuffs, count, beneficial, steal ? battleManager : null, battleManager);
                monster?.UpdateUI();
            }
        }

        battleManager.UpdateAllUI();
    }

    private static void RemoveFrom(List<Buff> buffs, int count, bool beneficial, TrainingBattleManager stealTo, TrainingBattleManager battleManager)
    {
        if (buffs == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            List<Buff> candidates = buffs.FindAll(buff => buff?.data != null && buff.stack > 0
                && (beneficial ? BuffData.IsRemovableBeneficialBuffId(buff.data.buffId) : BuffData.IsHarmfulBuffId(buff.data.buffId)));
            if (candidates.Count == 0)
            {
                return;
            }

            Buff removed = candidates[Random.Range(0, candidates.Count)];
            buffs.Remove(removed);
            if (stealTo != null)
            {
                stealTo.ApplyBuffToPlayer(removed.data.buffId, removed.stack);
            }
        }
    }
}
