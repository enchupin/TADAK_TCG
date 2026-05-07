using UnityEngine;

public class IceAndFireBossMonster : Monster
{
    private int nextPatternId;
    private bool hasHarmony;

    public override int MonsterId => 303;
    protected override string MonsterName => "얼음과 불";
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
                SetAttackIntent(previewDamage, $"피해를 {previewDamage}씩 10회 입힙니다.");
                SetPlannedPattern(30301, MonsterIntentIconType.Attack);
                break;
            case 30302:
                int debuffAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.FrailBuffId, 99);
                SetIntent($"적에게 빈약, 부식, 약화를 {debuffAmount}씩 부여합니다.");
                SetPlannedPattern(30302, MonsterIntentIconType.HarmfulEffect);
                break;
            default:
                int strengthAmount = PreviewMonsterBuffAmount(BattleRuntimeDefinitions.StrengthBuffId, 3);
                SetIntent($"힘을 {strengthAmount} 얻습니다.");
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
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.FrailBuffId, 99);
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.CorrosionBuffId, 99);
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.WeakBuffId, 99);
                break;
            default:
                AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 3);
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
                AddDefense(dealtDamage, false);
            }

            if (target != null && target.IsDead())
            {
                break;
            }
        }
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return PreviewOutgoingDamage(baseDamage);
    }
}
