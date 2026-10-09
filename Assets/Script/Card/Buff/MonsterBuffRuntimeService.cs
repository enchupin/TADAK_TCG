using System.Collections.Generic;
using UnityEngine;

public class MonsterBuffRuntimeService
{
    private readonly TrainingBattleManager battleManager;
    private readonly Dictionary<int, MonsterBuffScript> scripts = new();
    private readonly List<MonsterBuffScript> orderedScripts = new();

    public MonsterBuffRuntimeService(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;

        Register(new MonsterBuffs.FreezeBuffScript());
        Register(new MonsterBuffs.RegenerationBuffScript());
        Register(new MonsterBuffs.BurnBuffScript());
        Register(new MonsterBuffs.EnhancedCorrosionBuffScript());
        Register(new MonsterBuffs.CorrosionBuffScript());
        Register(new MonsterBuffs.StrengthBuffScript());
        Register(new MonsterBuffs.StrengthDecayBuffScript());
        Register(new MonsterBuffs.WeakBuffScript());
        Register(new MonsterBuffs.OverheatBuffScript());
        Register(new MonsterBuffs.RootedBuffScript());
        Register(new MonsterBuffs.GlacierBondBuffScript());
        Register(new MonsterBuffs.ThornBuffScript());
        Register(new MonsterBuffs.CrueltyBuffScript());
        Register(new MonsterBuffs.LifeLinkBuffScript());
        Register(new MonsterBuffs.FlameTransferBuffScript());
        Register(new MonsterBuffs.PoisonUpgradeBuffScript());
        Register(new MonsterBuffs.LifeStealBuffScript());
        Register(new MonsterBuffs.VoidShellBuffScript());
        Register(new MonsterBuffs.FaithfulPrayerBuffScript());
        Register(new MonsterBuffs.ParasiticMushroomBuffScript());
        Register(new MonsterBuffs.PranksterGhostBuffScript());
        Register(new MonsterBuffs.ThiefBuffScript());
        Register(new MonsterBuffs.BurningFlameBuffScript());
        Register(new MonsterBuffs.IntentionalRageBuffScript());
        Register(new MonsterBuffs.EightLegsBuffScript());
        Register(new MonsterBuffs.FuturePredationBuffScript());
        Register(new MonsterBuffs.HarmonyBuffScript());
        Register(new MonsterBuffs.AttackBoostBuffScript());
        Register(new MonsterBuffs.PoisonousMushroomBuffScript());
        Register(new MonsterBuffs.MirrorBuffScript());
        Register(new MonsterBuffs.DrowningBuffScript());
        Register(new MonsterBuffs.WhirlpoolBuffScript());
    }

    public int GetActivationCount(Monster monster, int buffId)
    {
        return scripts.TryGetValue(buffId, out MonsterBuffScript script) ? script.GetActivationCount(monster) : 0;
    }

    public void OnAttackActionStarted(Monster monster)
    {
        InvokeForActiveBuffs(monster, (script, stack) => script.OnAttackActionStarted(monster, stack));
    }

    public void OnAttackActionEnded(Monster monster)
    {
        foreach (MonsterBuffScript script in orderedScripts) script.OnAttackActionEnded(monster);
    }

    public void OnBuffApplied(Monster monster, int buffId, int appliedAmount)
    {
        if (monster == null || appliedAmount <= 0)
        {
            return;
        }

        if (!scripts.TryGetValue(buffId, out MonsterBuffScript script))
        {
            return;
        }

        int stack = monster.GetBuffStack(buffId);
        if (stack <= 0)
        {
            return;
        }

        script.OnBuffApplied(battleManager, monster, appliedAmount, stack);
    }

    public void OnMonsterTurnStart(Monster monster)
    {
        if (monster == null || monster.IsDead())
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) => script.OnMonsterTurnStart(battleManager, monster, stack));
    }

    public void OnMonsterTurnEnd(Monster monster)
    {
        if (monster == null || monster.IsDead())
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) => script.OnMonsterTurnEnd(battleManager, monster, stack));
    }

    public void OnPlayerTurnEnd()
    {
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            InvokeForActiveBuffs(monster, (script, stack) => script.OnPlayerTurnEnd(battleManager, monster, stack));
        }
    }

    public void OnEnemyDebuffApplied(Monster targetMonster, int buffId, int amount, int crueltyStackBeforeApply)
    {
        if (targetMonster == null || targetMonster.IsDead() || amount <= 0)
        {
            return;
        }

        InvokeForActiveBuffs(targetMonster, (script, stack) =>
            script.OnEnemyDebuffApplied(battleManager, targetMonster, buffId, amount, stack, crueltyStackBeforeApply));
    }

    public void OnMonsterHpLost(Monster targetMonster, int hpLoss)
    {
        if (targetMonster == null || hpLoss <= 0)
        {
            return;
        }

        InvokeForActiveBuffs(targetMonster, (script, stack) => script.OnMonsterHpLost(battleManager, targetMonster, hpLoss, stack));
    }

    public void OnPlayerHpLost(int hpLoss)
    {
        if (hpLoss <= 0)
        {
            return;
        }

        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            InvokeForActiveBuffs(monster, (script, stack) => script.OnPlayerHpLost(battleManager, monster, hpLoss, stack));
        }
    }

    public void OnMonsterDeath(Monster deadMonster)
    {
        if (deadMonster == null)
        {
            return;
        }

        InvokeForActiveBuffs(deadMonster, (script, stack) => script.OnMonsterDeath(battleManager, deadMonster, stack));
    }

    public void OnMonsterLeaveCombat(Monster monster)
    {
        if (monster == null)
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) => script.OnMonsterLeaveCombat(battleManager, monster, stack));
    }

    public void OnMonsterRevived(Monster monster)
    {
        if (monster == null || monster.IsDead())
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) => script.OnMonsterRevived(battleManager, monster, stack));
    }

    public int ResolvePersistentUpgradeCardId(int cardId, int sourceBuffId = 0)
    {
        int resolvedCardId = cardId;
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            InvokeForActiveBuffs(monster, (script, stack) =>
            {
                resolvedCardId = script.ResolvePersistentUpgradeCardId(battleManager, monster, cardId, resolvedCardId, sourceBuffId, stack);
            });
        }

        return resolvedCardId;
    }

    public bool HasAnyLivingMonsterWithBuff(int buffId)
    {
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            if (monster != null && !monster.IsDead() && monster.GetBuffStack(buffId) > 0)
            {
                return true;
            }
        }

        return false;
    }

    public void OnMonsterBeforeTakeDamage(Monster monster, int incomingDamage)
    {
        if (monster == null || incomingDamage <= 0)
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            script.OnMonsterBeforeTakeDamage(battleManager, monster, incomingDamage, stack);
        });
    }

    public void OnMonsterAfterTakeDamage(Monster monster, int incomingDamage, int damageAfterDefense)
    {
        if (monster == null || incomingDamage <= 0)
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            script.OnMonsterAfterTakeDamage(battleManager, monster, incomingDamage, damageAfterDefense, stack);
        });
    }

    public float GetIncomingDamageMultiplier(Monster monster)
    {
        if (monster == null)
        {
            return 1f;
        }

        float multiplier = 1f;
        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            multiplier = script.GetIncomingDamageMultiplier(battleManager, monster, stack, multiplier);
        });

        return multiplier;
    }

    public bool TryConsumeIncomingDamageBuff(Monster monster)
    {
        if (monster == null)
        {
            return false;
        }

        foreach (MonsterBuffScript script in orderedScripts)
        {
            int stack = monster.GetBuffStack(script.BuffId);
            if (stack <= 0)
            {
                continue;
            }

            if (script.TryConsumeIncomingDamageBuff(battleManager, monster, stack))
            {
                return true;
            }
        }

        return false;
    }

    public bool ShouldKeepBarrierOnTurnStart(Monster monster)
    {
        if (monster == null)
        {
            return false;
        }

        bool shouldKeep = false;
        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            shouldKeep = script.ShouldKeepBarrierOnTurnStart(battleManager, monster, stack, shouldKeep);
        });

        return shouldKeep;
    }

    public void OnDefenseChanged(Monster monster, int previousDefense, int currentDefense)
    {
        if (monster == null)
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            script.OnDefenseChanged(battleManager, monster, previousDefense, currentDefense, stack);
        });
    }

    public int ModifyOutgoingDamage(Monster monster, int damage)
    {
        if (monster == null || damage <= 0)
        {
            return 0;
        }

        int flatBonus = 0;
        float damageMultiplier = 1f;

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            flatBonus = script.GetOutgoingDamageFlatBonus(battleManager, monster, stack, flatBonus);
        });

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            damageMultiplier = script.GetOutgoingDamageMultiplier(battleManager, monster, stack, damageMultiplier);
        });

        if (monster.GetBuffStack(BattleRuntimeDefinitions.IntentionalRageBuffId) <= 0
            && HasOtherLivingMonsterWithBuff(monster, BattleRuntimeDefinitions.IntentionalRageBuffId))
        {
            damageMultiplier *= 0.5f;
        }

        int finalDamage = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0, damage + flatBonus) * Mathf.Max(0f, damageMultiplier)));

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            finalDamage = script.ModifyOutgoingDamage(battleManager, monster, stack, finalDamage);
        });

        return finalDamage;
    }

    public void OnMonsterAttackResolved(Monster monster, PlayerData target, int attemptedDamage, int hpDamage)
    {
        if (monster == null)
        {
            return;
        }

        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            script.OnMonsterAttackResolved(battleManager, monster, target, attemptedDamage, hpDamage, stack);
        });
    }

    public int ResolveBarrierGain(Monster monster, int amount)
    {
        if (monster == null || amount <= 0)
        {
            return 0;
        }

        int finalAmount = amount;
        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            finalAmount = script.ModifyBarrierGain(battleManager, monster, stack, finalAmount);
        });

        return finalAmount;
    }

    public bool CanReceiveDamage(Monster monster, int incomingDamage)
    {
        if (monster == null)
        {
            return true;
        }

        bool canReceive = true;
        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            canReceive = script.CanReceiveDamage(battleManager, monster, incomingDamage, stack, canReceive);
        });

        return canReceive;
    }

    public bool CanRevive(Monster monster)
    {
        if (monster == null)
        {
            return false;
        }

        bool canRevive = true;
        InvokeForActiveBuffs(monster, (script, stack) =>
        {
            canRevive = script.CanRevive(battleManager, monster, stack, canRevive);
        });

        return canRevive;
    }

    private bool HasOtherLivingMonsterWithBuff(Monster excludedMonster, int buffId)
    {
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            if (monster == null || monster == excludedMonster || monster.IsDead())
            {
                continue;
            }

            if (monster.GetBuffStack(buffId) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void Register(MonsterBuffScript script)
    {
        if (script == null || script.BuffId <= 0 || scripts.ContainsKey(script.BuffId))
        {
            return;
        }

        scripts[script.BuffId] = script;
        orderedScripts.Add(script);
    }

    private void InvokeForActiveBuffs(Monster monster, System.Action<MonsterBuffScript, int> action)
    {
        if (monster == null || action == null)
        {
            return;
        }

        foreach (MonsterBuffScript script in orderedScripts)
        {
            int stack = monster.GetBuffStack(script.BuffId);
            if (stack > 0)
            {
                action(script, stack);
            }
        }
    }
}
