using UnityEngine;

public class MutantSweetPotatoMonster : Monster
{
    private const int RootPatternId = 11201;
    private const int AttackDebuffPatternId = 11202;
    private const int MultiHitPatternId = 11203;
    private const int PowerUpPatternId = 11204;
    private const int RootBarrierAmount = 30;
    private const int AttackBaseDamage = 12;
    private const int AttackRootedDamage = 15;
    private const int AttackBaseDebuffStack = 2;
    private const int AttackRootedDebuffStack = 3;
    private const int MultiHitBaseDamage = 8;
    private const int MultiHitBaseCount = 2;
    private const int MultiHitRootedDamage = 8;
    private const int MultiHitRootedCount = 3;
    private const int DamageAmplifyAmount = 2;

    private int phase;
    private int plannedPatternIdForTurn;

    public override int MonsterId => 112;
    protected override string MonsterName => "변이 고구마";
    protected override int BaseMaxHp => 48;

    protected override void OnBattleStart()
    {
        phase = 0;
        plannedPatternIdForTurn = 0;
    }

    protected override bool IsNonStackableBuff(int buffId)
    {
        return buffId == BattleRuntimeDefinitions.RootedBuffId || base.IsNonStackableBuff(buffId);
    }

    protected override void BuildNextAction()
    {
        plannedPatternIdForTurn = GetPatternIdForCurrentPhase();
        switch (plannedPatternIdForTurn)
        {
            case RootPatternId:
                SetIntent($"뿌리내림을 얻습니다. 보호막을 {RootBarrierAmount} 얻습니다.");
                SetPlannedPattern(RootPatternId, MonsterIntentIconType.Protection, MonsterIntentIconType.BeneficialEffect);
                break;
            case MultiHitPatternId:
                int multiHitDamage = GetPreviewDamage(IsRooted() ? MultiHitRootedDamage : MultiHitBaseDamage);
                int multiHitCount = IsRooted() ? MultiHitRootedCount : MultiHitBaseCount;
                SetAttackIntent(multiHitDamage, $"피해를 {multiHitDamage}씩 {multiHitCount}회 입힙니다.");
                SetPlannedPattern(MultiHitPatternId, MonsterIntentIconType.Attack);
                break;
            case PowerUpPatternId:
                SetIntent($"피해 증폭을 {DamageAmplifyAmount} 얻습니다.");
                SetPlannedPattern(PowerUpPatternId, MonsterIntentIconType.BeneficialEffect);
                break;
            default:
                int attackDamage = GetPreviewDamage(IsRooted() ? AttackRootedDamage : AttackBaseDamage);
                int debuffStack = IsRooted() ? AttackRootedDebuffStack : AttackBaseDebuffStack;
                SetAttackIntent(attackDamage, $"피해를 {attackDamage} 입힙니다. 빈약을 {debuffStack} 부여합니다.");
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
                AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, DamageAmplifyAmount);
                break;
            default:
                DealDamage(target, IsRooted() ? AttackRootedDamage : AttackBaseDamage);
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, IsRooted() ? AttackRootedDebuffStack : AttackBaseDebuffStack);
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
        return Mathf.Max(0, baseDamage + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
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
