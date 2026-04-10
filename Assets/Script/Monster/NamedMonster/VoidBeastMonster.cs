using UnityEngine;

public class VoidBeastMonster : Monster
{
    private int patternIndex;
    private int attackHitCount = 2;

    public override int MonsterId => 204;
    protected override string MonsterName => "공허 괴수";
    protected override int BaseMaxHp => 200;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.VoidShellBuffId, 7);
    }

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                SetAttackIntent(GetPreviewDamage(8), $"피해를 {GetPreviewDamage(8)}씩 {attackHitCount}회 입힙니다.");
                SetPlannedPattern(20401, MonsterIntentIconType.Attack);
                break;
            case 1:
                SetIntent("적의 힘을 2 감소시키고, 힘을 2 얻습니다.");
                SetPlannedPattern(20402, MonsterIntentIconType.HarmfulEffect, MonsterIntentIconType.BeneficialEffect);
                break;
            default:
                SetIntent("공허 껍질을 3 얻습니다.");
                SetPlannedPattern(20403, MonsterIntentIconType.BeneficialEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                for (int hitIndex = 0; hitIndex < attackHitCount; hitIndex++)
                {
                    DealDamage(target, 8);
                    if (target != null && target.IsDead())
                    {
                        break;
                    }
                }

                attackHitCount++;
                break;
            case 1:
                target?.ConsumeBuffStack(BattleRuntimeDefinitions.StrengthBuffId, 2);
                AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 2);
                break;
            default:
                AddBuff(BattleRuntimeDefinitions.VoidShellBuffId, 3);
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return PreviewOutgoingDamage(baseDamage);
    }
}
