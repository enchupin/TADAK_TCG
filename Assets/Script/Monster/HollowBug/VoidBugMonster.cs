using UnityEngine;

public class VoidBugMonster : Monster
{
    private bool useAttackPattern = true;
    private bool hasTriggeredVoidShellThisTurn;

    public override int MonsterId => 109;
    protected override string MonsterName => "공허 벌레";
    protected override int BaseMaxHp => 45;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.VoidShellBuffId, 4);
    }

    protected override void OnTurnStarted()
    {
        hasTriggeredVoidShellThisTurn = false;
    }

    protected override void OnBeforeTakeDamage(int incomingDamage)
    {
        if (incomingDamage <= 0 || hasTriggeredVoidShellThisTurn)
        {
            return;
        }

        int voidShellStack = GetBuffStack(BattleRuntimeDefinitions.VoidShellBuffId);
        if (voidShellStack <= 0)
        {
            return;
        }

        hasTriggeredVoidShellThisTurn = true;
        AddDefense(voidShellStack);
    }

    protected override void BuildNextAction()
    {
        if (useAttackPattern)
        {
            int nextPatternId = Random.value < 0.5f ? 10901 : 10902;
            if (nextPatternId == 10901)
            {
                SetAttackIntent(1, "피해를 1 입히고 적에게 부식을 3 부여합니다.");
                SetPlannedPattern(10901, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
                return;
            }

            SetAttackIntent(1, "피해를 1 입히고 적에게 빈약을 3 부여합니다.");
            SetPlannedPattern(10902, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
            return;
        }

        SetIntent("피해 증폭을 7 얻습니다.");
        SetPlannedPattern(10903, MonsterIntentIconType.BeneficialEffect);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (PlannedPatternId)
        {
            case 10901:
                DealDamage(target, 1);
                target?.AddBuff(BattleRuntimeDefinitions.CorrosionBuffId, 3);
                break;
            case 10902:
                DealDamage(target, 1);
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 3);
                break;
            default:
                AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 7);
                break;
        }

        useAttackPattern = !useAttackPattern;
    }
}
