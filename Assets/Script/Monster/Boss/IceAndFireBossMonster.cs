using UnityEngine;

public class IceAndFireBossMonster : Monster
{
    private int nextPatternId;
    private bool hasHarmony;

    public override int MonsterId => 303;
    protected override string MonsterName => "\uC5BC\uC74C\uACFC \uBD88";
    protected override int BaseMaxHp => 300;
    protected override bool IsBossMonster => true;

    protected override void OnBattleStart()
    {
        hasHarmony = true;
        nextPatternId = 30302;
        AddDefense(100);
    }

    protected override void OnTurnEnded()
    {
        if (IsDead())
        {
            return;
        }

        nextPatternId = defense > 0 ? 30303 : 30301;
    }

    protected override void BuildNextAction()
    {
        switch (nextPatternId)
        {
            case 30301:
                int previewDamage = GetPreviewDamage(2);
                SetAttackIntent(previewDamage, $"\uD53C\uD574\uB97C {previewDamage}\uC529 10\uD68C \uC785\uD799\uB2C8\uB2E4.");
                SetPlannedPattern(30301, MonsterIntentIconType.Attack);
                break;
            case 30302:
                SetIntent("\uC801\uC5D0\uAC8C \uBE48\uC57D, \uBD80\uC2DD, \uC57D\uD654\uB97C 99\uC529 \uBD80\uC5EC\uD569\uB2C8\uB2E4.");
                SetPlannedPattern(30302, MonsterIntentIconType.HarmfulEffect);
                break;
            default:
                SetIntent("\uD53C\uD574 \uC99D\uD3ED\uC744 3 \uC5BB\uC2B5\uB2C8\uB2E4.");
                SetPlannedPattern(30303, MonsterIntentIconType.BeneficialEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (nextPatternId)
        {
            case 30301:
                ExecuteHarmonyAttack(target);
                break;
            case 30302:
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 99);
                target?.AddBuff(BattleRuntimeDefinitions.CorrosionBuffId, 99);
                target?.AddBuff(BattleRuntimeDefinitions.WeakBuffId, 99);
                break;
            default:
                AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 3);
                break;
        }
    }

    private void ExecuteHarmonyAttack(PlayerData target)
    {
        for (int hitIndex = 0; hitIndex < 10; hitIndex++)
        {
            int dealtDamage = DealDamage(target, 2);
            if (hasHarmony && dealtDamage > 0)
            {
                AddDefense(dealtDamage);
            }

            if (target != null && target.IsDead())
            {
                break;
            }
        }
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
    }
}
