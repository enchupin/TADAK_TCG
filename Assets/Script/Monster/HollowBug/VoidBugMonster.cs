using UnityEngine;

public class VoidBugMonster : Monster
{
    private bool useAttackPattern = true;

    public override int MonsterId => 109;
    protected override string MonsterName => "怨듯뿀 踰뚮젅";
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
                SetAttackIntent(previewDamage, $"?쇳빐瑜?{previewDamage} ?낇엳怨??곸뿉寃?遺?앹쓣 3 遺?ы빀?덈떎.");
                SetPlannedPattern(10901, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
                return;
            }

            SetAttackIntent(previewDamage, $"?쇳빐瑜?{previewDamage} ?낇엳怨??곸뿉寃?鍮덉빟??3 遺?ы빀?덈떎.");
            SetPlannedPattern(10902, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
            return;
        }

        SetIntent("힘을 7 얻습니다.");
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
                AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 7);
                break;
        }

        useAttackPattern = !useAttackPattern;
    }
}
