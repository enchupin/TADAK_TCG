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

        Register(new FreezeMonsterBuffScript());
        Register(new RegenerationMonsterBuffScript());
        Register(new BurnMonsterBuffScript());
        Register(new EnhancedCorrosionMonsterBuffScript());
        Register(new CorrosionMonsterBuffScript());
        Register(new StrengthMonsterBuffScript());
        Register(new StrengthDecayMonsterBuffScript());
        Register(new WeakMonsterBuffScript());
        Register(new RootedMonsterBuffScript());
        Register(new GlacierBondMonsterBuffScript());
        Register(new ThornMonsterBuffScript());
        Register(new CrueltyMonsterBuffScript());
        Register(new LifeLinkMonsterBuffScript());
        Register(new FlameTransferMonsterBuffScript());
        Register(new PoisonUpgradeMonsterBuffScript());
        Register(new MonsterLifeStealMonsterBuffScript());
        Register(new VoidShellMonsterBuffScript());
        Register(new FaithfulPrayerMonsterBuffScript());
        Register(new ParasiticMushroomMonsterBuffScript());
        Register(new PranksterGhostMonsterBuffScript());
        Register(new ThiefMonsterBuffScript());
        Register(new BurningFlameMonsterBuffScript());
        Register(new EightLegsMonsterBuffScript());
        Register(new FuturePredationMonsterBuffScript());
        Register(new PoisonousMushroomMonsterBuffScript());
        Register(new MirrorMonsterBuffScript());
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
