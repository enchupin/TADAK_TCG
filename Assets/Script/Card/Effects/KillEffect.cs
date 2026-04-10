using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KillEffect : ICardEffect
{
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        ExecuteInternal(battleManager);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager)
    {
        if (battleManager == null) {
            return;
        }

        List<Monster> targets = ResolveTargets(battleManager);
        if (targets.Count == 0) {
            return;
        }

        foreach (Monster monster in targets) {
            if (monster == null || monster.IsDead()) {
                continue;
            }

            monster.Kill();
        }

        battleManager.UpdateAllUI();
    }

    private List<Monster> ResolveTargets(TrainingBattleManager battleManager)
    {
        List<Monster> targets = new List<Monster>();

        switch (target)
        {
            case TargetType.SingleEnemy:
                Monster singleTarget = battleManager.currentTarget;
                if (singleTarget == null) {
                    List<Monster> livingMonsters = battleManager.GetLivingMonsters();
                    if (livingMonsters != null && livingMonsters.Count == 1) {
                        singleTarget = livingMonsters[0];
                    }
                }

                if (singleTarget == null || singleTarget.IsDead()) {
                    Debug.LogWarning("[KillEffect] SingleEnemy 대상이 없습니다");
                    return targets;
                }

                targets.Add(singleTarget);
                return targets;

            case TargetType.AllEnemies:
                List<Monster> livingTargets = battleManager.GetLivingMonsters();
                if (livingTargets == null || livingTargets.Count == 0) {
                    Debug.LogWarning("[KillEffect] 처치할 적이 없습니다");
                    return targets;
                }

                targets.AddRange(livingTargets);
                return targets;

            default:
                Debug.LogWarning($"[KillEffect] 지원하지 않는 대상 타입입니다: {target}");
                return targets;
        }
    }
}

[System.Serializable]
public class PartyEffect : ICardEffect
{
    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count < 2)
        {
            Debug.LogWarning("[PartyEffect] 파티를 만들 적이 부족합니다");
            return;
        }

        battleManager.OpenMonsterSelection(livingMonsters, 2, selectedMonsters => ApplyParty(battleManager, selectedMonsters));
    }

    private static void ApplyParty(TrainingBattleManager battleManager, List<Monster> selectedMonsters)
    {
        if (battleManager == null || selectedMonsters == null)
        {
            return;
        }

        List<Monster> validTargets = new List<Monster>();
        foreach (Monster monster in selectedMonsters)
        {
            if (monster == null || monster.IsDead() || validTargets.Contains(monster))
            {
                continue;
            }

            validTargets.Add(monster);
        }

        if (validTargets.Count < 2)
        {
            return;
        }

        Monster firstMonster = validTargets[0];
        Monster secondMonster = validTargets[1];
        Monster survivor = ResolvePartySurvivor(firstMonster, secondMonster);
        Monster absorbedMonster = survivor == firstMonster ? secondMonster : firstMonster;

        MergeMonsterState(survivor, absorbedMonster);
        absorbedMonster.currentBuffs.Clear();
        absorbedMonster.Kill();
        survivor.UpdateUI();
        battleManager.UpdateAllUI();
    }

    private static Monster ResolvePartySurvivor(Monster firstMonster, Monster secondMonster)
    {
        if (firstMonster == null)
        {
            return secondMonster;
        }

        if (secondMonster == null)
        {
            return firstMonster;
        }

        if (firstMonster.MonsterId != secondMonster.MonsterId)
        {
            return firstMonster.MonsterId > secondMonster.MonsterId ? firstMonster : secondMonster;
        }

        return firstMonster.GetInstanceID() >= secondMonster.GetInstanceID() ? firstMonster : secondMonster;
    }

    private static void MergeMonsterState(Monster survivor, Monster absorbedMonster)
    {
        if (survivor == null || absorbedMonster == null)
        {
            return;
        }

        survivor.maxHP = Mathf.Max(1, survivor.maxHP + Mathf.Max(0, absorbedMonster.maxHP));
        survivor.hp = Mathf.Clamp(survivor.hp + Mathf.Max(0, absorbedMonster.hp), 0, survivor.maxHP);

        foreach (Buff absorbedBuff in absorbedMonster.currentBuffs)
        {
            if (absorbedBuff?.data == null || absorbedBuff.stack <= 0)
            {
                continue;
            }

            MergeBuff(survivor, absorbedBuff.data.buffId, absorbedBuff.stack);
        }
    }

    private static void MergeBuff(Monster survivor, int buffId, int stack)
    {
        if (survivor == null || buffId <= 0 || stack <= 0)
        {
            return;
        }

        Buff existingBuff = survivor.currentBuffs.Find(candidate =>
            candidate?.data != null &&
            candidate.data.buffId == buffId);
        int mergedStack = BuffData.IsNonStackableBuffId(buffId) ? 1 : stack;
        if (existingBuff != null)
        {
            existingBuff.data = BuffMetadataResolver.Resolve(buffId);
            existingBuff.stack = BuffData.IsNonStackableBuffId(buffId)
                ? 1
                : existingBuff.stack + mergedStack;
            return;
        }

        survivor.currentBuffs.Add(new Buff(BuffMetadataResolver.Resolve(buffId), mergedStack));
    }
}
