using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MixBuffEffect : ICardEffect
{
    public TargetType target = TargetType.Self;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        switch (target)
        {
            case TargetType.Self:
            case TargetType.None:
                MixBuffs(battleManager.playerData?.currentBuffs);
                break;

            case TargetType.SingleEnemy:
            case TargetType.AllEnemies:
                List<Monster> targets = CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
                foreach (Monster monster in targets)
                {
                    if (monster == null)
                    {
                        continue;
                    }

                    MixBuffs(monster.currentBuffs);
                    monster.UpdateUI();
                }
                break;
        }

        battleManager.UpdateAllUI();
    }

    private static void MixBuffs(List<Buff> buffs)
    {
        if (buffs == null || buffs.Count == 0)
        {
            return;
        }

        List<Buff> activeBuffs = new List<Buff>();
        int totalStack = 0;

        foreach (Buff buff in buffs)
        {
            if (buff == null || buff.data == null || buff.stack <= 0)
            {
                continue;
            }

            activeBuffs.Add(buff);
            totalStack += buff.stack;
        }

        if (activeBuffs.Count == 0 || totalStack <= 0)
        {
            return;
        }

        foreach (Buff buff in activeBuffs)
        {
            buff.stack = 1;
        }

        int remainingStack = totalStack - activeBuffs.Count;
        while (remainingStack > 0)
        {
            int randomIndex = Random.Range(0, activeBuffs.Count);
            activeBuffs[randomIndex].stack++;
            remainingStack--;
        }
    }
}
