public class MutantMushroomMonster : Monster
{
    private bool useDrainAttack = true;

    public override int MonsterId => 103;
    protected override string MonsterName => "변이 버섯";
    protected override int BaseMaxHp => 27;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.PoisonousMushroomBuffId, 1);
    }

    protected override bool IsNonStackableBuff(int buffId)
    {
        return buffId == BattleRuntimeDefinitions.PoisonousMushroomBuffId || base.IsNonStackableBuff(buffId);
    }

    protected override void OnDeathTriggered()
    {
        if (GetBuffStack(BattleRuntimeDefinitions.PoisonousMushroomBuffId) <= 0)
        {
            return;
        }

        TrainingBattleManager.Instance?.playerData?.AddBuff(BattleRuntimeDefinitions.EnhancedCorrosionBuffId, 2);
    }

    protected override void BuildNextAction()
    {
        if (useDrainAttack)
        {
            SetAttackIntent(7, "Deal 7 damage. Heal equal to unblocked damage dealt");
            SetPlannedPattern(10301, MonsterIntentIconType.Attack);
            return;
        }

        SetIntent("Gain 12 Barrier. Apply 2 Weak | Buff");
        SetPlannedPattern(10302, MonsterIntentIconType.Attack, MonsterIntentIconType.BeneficialEffect);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (useDrainAttack)
        {
            int unblockedDamage = DealDamage(target, 7);
            if (unblockedDamage > 0)
            {
                Heal(unblockedDamage);
            }
        }
        else
        {
            AddDefense(12);
            target?.AddBuff(BattleRuntimeDefinitions.WeakBuffId, 2);
        }

        useDrainAttack = !useDrainAttack;
    }
}
