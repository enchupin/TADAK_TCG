using UnityEngine;

public class MutantCarrotMonster : Monster
{
    private const int RootPatternId = 11301;
    private const int AttackDebuffPatternId = 11302;
    private const int MultiHitPatternId = 11303;
    private const int PowerUpPatternId = 11304;
    private const int RootBarrierAmount = 36;
    private const int AttackBaseDamage = 15;
    private const int AttackRootedDamage = 18;
    private const int AttackBaseDebuffStack = 2;
    private const int AttackRootedDebuffStack = 3;
    private const int MultiHitBaseDamage = 9;
    private const int MultiHitBaseCount = 2;
    private const int MultiHitRootedDamage = 10;
    private const int MultiHitRootedCount = 3;
    private const int StrengthAmount = 3;

    private int phase;
    private int plannedPatternIdForTurn;

    public override int MonsterId => 113;
    protected override string MonsterName => "변이 당근";
    protected override int BaseMaxHp => 52;

    protected override void OnBattleStart()
    {
        phase = 0;
        plannedPatternIdForTurn = 0;
    }

    protected override void BuildNextAction()
    {
        plannedPatternIdForTurn = GetPatternIdForCurrentPhase();
        switch (plannedPatternIdForTurn)
        {
            case RootPatternId:
                int rootBarrierGain = PreviewBarrierGain(RootBarrierAmount);
                SetIntent($"뿌리내림을 얻습니다. 보호막을 {rootBarrierGain} 얻습니다.");
                SetPlannedPattern(RootPatternId, MonsterIntentIconType.Protection, MonsterIntentIconType.BeneficialEffect);
                break;
            case MultiHitPatternId:
                int multiHitDamage = GetPreviewDamage(IsRooted() ? MultiHitRootedDamage : MultiHitBaseDamage);
                int multiHitCount = IsRooted() ? MultiHitRootedCount : MultiHitBaseCount;
                SetAttackIntent(multiHitDamage, $"피해를 {multiHitDamage}씩 {multiHitCount}회 입힙니다.");
                SetPlannedPattern(MultiHitPatternId, MonsterIntentIconType.Attack);
                break;
            case PowerUpPatternId:
                int strengthAmount = PreviewMonsterBuffAmount(BattleRuntimeDefinitions.StrengthBuffId, StrengthAmount);
                SetIntent($"힘을 {strengthAmount} 얻습니다.");
                SetPlannedPattern(PowerUpPatternId, MonsterIntentIconType.BeneficialEffect);
                break;
            default:
                int attackDamage = GetPreviewDamage(IsRooted() ? AttackRootedDamage : AttackBaseDamage);
                int debuffStack = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.WeakBuffId, IsRooted() ? AttackRootedDebuffStack : AttackBaseDebuffStack);
                SetAttackIntent(attackDamage, $"피해를 {attackDamage} 입힙니다. 약화를 {debuffStack} 부여합니다.");
                SetPlannedPattern(AttackDebuffPatternId, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (plannedPatternIdForTurn)
        {
            case RootPatternId:
                AddBuff(BattleRuntimeDefinitions.RootedBuffId, 1);
                AddDefense(RootBarrierAmount);
                break;
            case MultiHitPatternId:
                int multiHitDamage = IsRooted() ? MultiHitRootedDamage : MultiHitBaseDamage;
                int multiHitCount = IsRooted() ? MultiHitRootedCount : MultiHitBaseCount;
                for (int hitIndex = 0; hitIndex < multiHitCount; hitIndex++)
                {
                    DealDamage(target, multiHitDamage);
                    if (target != null && target.IsDead())
                    {
                        break;
                    }
                }
                break;
            case PowerUpPatternId:
                AddBuff(BattleRuntimeDefinitions.StrengthBuffId, StrengthAmount);
                break;
            default:
                DealDamage(target, IsRooted() ? AttackRootedDamage : AttackBaseDamage);
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.WeakBuffId, IsRooted() ? AttackRootedDebuffStack : AttackBaseDebuffStack);
                break;
        }

        AdvancePhase();
    }

    private bool IsRooted()
    {
        return GetBuffStack(BattleRuntimeDefinitions.RootedBuffId) > 0;
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return PreviewOutgoingDamage(baseDamage);
    }

    private int GetPatternIdForCurrentPhase()
    {
        switch (phase)
        {
            case 1:
                return RootPatternId;
            case 3:
                return MultiHitPatternId;
            case 4:
                return PowerUpPatternId;
            default:
                return AttackDebuffPatternId;
        }
    }

    private void AdvancePhase()
    {
        switch (phase)
        {
            case 0:
                phase = 1;
                break;
            case 1:
            case 4:
                phase = 2;
                break;
            default:
                phase++;
                break;
        }
    }
}
