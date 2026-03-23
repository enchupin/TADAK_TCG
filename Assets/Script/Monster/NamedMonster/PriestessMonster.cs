using UnityEngine;

public class PriestessMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 202;
    protected override string MonsterName => "여사제";
    protected override int BaseMaxHp => 160;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.FaithfulPrayerBuffId, 1);
    }

    protected override bool IsNonStackableBuff(int buffId)
    {
        return buffId == BattleRuntimeDefinitions.FaithfulPrayerBuffId || base.IsNonStackableBuff(buffId);
    }

    protected override void OnTurnEnded()
    {
        if (GetBuffStack(BattleRuntimeDefinitions.FaithfulPrayerBuffId) <= 0)
        {
            return;
        }

        AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 1);
        AddBuff(BattleRuntimeDefinitions.GlacierBondBuffId, 1);
    }

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                SetAttackIntent(GetPreviewDamage(13), $"피해를 {GetPreviewDamage(13)} 입힙니다. 보호막을 {GetBarrierGain(8)} 얻습니다.");
                SetPlannedPattern(20201, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection);
                break;
            case 1:
                SetAttackIntent(GetPreviewDamage(4), $"피해를 {GetPreviewDamage(4)}씩 2회 입힙니다.");
                SetPlannedPattern(20202, MonsterIntentIconType.Attack);
                break;
            case 2:
                SetIntent($"보호막을 {GetBarrierGain(12)} 얻습니다.");
                SetPlannedPattern(20203, MonsterIntentIconType.Protection);
                break;
            default:
                SetAttackIntent(GetPreviewDamage(25), $"피해를 {GetPreviewDamage(25)} 입힙니다.");
                SetPlannedPattern(20204, MonsterIntentIconType.Attack);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                DealDamage(target, 13);
                AddDefense(GetBarrierGain(8));
                break;
            case 1:
                for (int hitIndex = 0; hitIndex < 2; hitIndex++)
                {
                    DealDamage(target, 4);
                    if (target != null && target.IsDead())
                    {
                        break;
                    }
                }
                break;
            case 2:
                AddDefense(GetBarrierGain(12));
                break;
            default:
                DealDamage(target, 25);
                break;
        }

        patternIndex = (patternIndex + 1) % 4;
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
    }

    private int GetBarrierGain(int baseAmount)
    {
        return Mathf.Max(0, baseAmount + GetBuffStack(BattleRuntimeDefinitions.GlacierBondBuffId));
    }
}
