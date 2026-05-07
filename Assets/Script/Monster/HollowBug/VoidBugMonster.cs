using UnityEngine;

public class VoidBugMonster : Monster
{
    private bool useAttackPattern = true;

    public override int MonsterId => 109;
    protected override string MonsterName => "공허 벌레";
    protected override int BaseMaxHp => 45;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.VoidShellBuffId, 4);
    }

    protected override void BuildNextAction()
    {
        if (useAttackPattern)
        {
            int previewDamage = PreviewOutgoingDamage(1);
            int nextPatternId = Random.value < 0.5f ? 10901 : 10902;
            if (nextPatternId == 10901)
            {
                int corrosionAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.CorrosionBuffId, 3);
                SetAttackIntent(previewDamage, $"피해를 {previewDamage} 입히고 적에게 부식을 {corrosionAmount} 부여합니다.");
                SetPlannedPattern(10901, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
                return;
            }

            int frailAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.FrailBuffId, 3);
            SetAttackIntent(previewDamage, $"피해를 {previewDamage} 입히고 적에게 빈약을 {frailAmount} 부여합니다.");
            SetPlannedPattern(10902, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
            return;
        }

        int strengthAmount = PreviewMonsterBuffAmount(BattleRuntimeDefinitions.StrengthBuffId, 7);
        SetIntent($"힘을 {strengthAmount} 얻습니다.");
        SetPlannedPattern(10903, MonsterIntentIconType.BeneficialEffect);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (PlannedPatternId)
        {
            case 10901:
                DealDamage(target, 1);
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.CorrosionBuffId, 3);
                break;
            case 10902:
                DealDamage(target, 1);
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.FrailBuffId, 3);
                break;
            default:
                AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 7);
                break;
        }

        useAttackPattern = !useAttackPattern;
    }
}
