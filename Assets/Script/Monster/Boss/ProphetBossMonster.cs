using System.Collections.Generic;
using UnityEngine;

public class ProphetBossMonster : Monster
{
    private int plannedPatternIdForTurn;
    private int plannedDamageValue;
    private int plannedRepeatCount;
    private int routeStage;
    private bool stealSucceededLastCycle;

    public override int MonsterId => 302;
    protected override string MonsterName => "선지자";
    protected override int BaseMaxHp => 250;
    protected override bool IsBossMonster => true;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.FuturePredationBuffId, 1);
        plannedPatternIdForTurn = 0;
        plannedDamageValue = 0;
        plannedRepeatCount = 1;
        routeStage = 0;
        stealSucceededLastCycle = false;
    }

    protected override void BuildNextAction()
    {
        plannedPatternIdForTurn = GetPatternIdForCurrentRoute();
        plannedRepeatCount = Mathf.Max(1, GetBuffStack(BattleRuntimeDefinitions.FuturePredationBuffId));
        plannedDamageValue = 0;

        switch (plannedPatternIdForTurn)
        {
            case 30201:
                SetIntent("적의 파워 능력을 하나 훔칩니다. 파워 능력이 없다면 다음 공격이 매우 강해집니다.");
                SetPlannedPattern(30201, MonsterIntentIconType.BeneficialEffect, MonsterIntentIconType.HarmfulEffect);
                break;
            case 30202:
                plannedDamageValue = GetPreviewDamage(8);
                SetAttackIntent(plannedDamageValue, $"피해를 {plannedDamageValue} 입힙니다. {plannedRepeatCount}회 반복합니다.");
                SetPlannedPattern(30202, MonsterIntentIconType.Attack);
                break;
            case 30203:
                int frailAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.FrailBuffId, 2);
                SetIntent($"빈약을 {frailAmount} 부여합니다.");
                SetPlannedPattern(30203, MonsterIntentIconType.HarmfulEffect);
                break;
            case 30204:
                plannedDamageValue = GetPreviewDamage(40);
                SetAttackIntent(plannedDamageValue, $"피해를 {plannedDamageValue} 입힙니다.");
                SetPlannedPattern(30204, MonsterIntentIconType.Attack);
                break;
            default:
                int corrosionAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.CorrosionBuffId, 2);
                SetIntent($"부식을 {corrosionAmount} 부여합니다.");
                SetPlannedPattern(30205, MonsterIntentIconType.HarmfulEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (plannedPatternIdForTurn)
        {
            case 30201:
                stealSucceededLastCycle = TryStealPlayerBeneficialBuff();
                routeStage = 1;
                break;
            case 30202:
                for (int repeatIndex = 0; repeatIndex < plannedRepeatCount; repeatIndex++)
                {
                    DealFixedDamage(target, plannedDamageValue);
                    if (target != null && target.IsDead())
                    {
                        break;
                    }
                }

                routeStage = 2;
                break;
            case 30203:
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.FrailBuffId, 2);
                routeStage = 0;
                break;
            case 30204:
                DealFixedDamage(target, plannedDamageValue);
                routeStage = 2;
                break;
            default:
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.CorrosionBuffId, 2);
                routeStage = 0;
                break;
        }
    }

    private bool TryStealPlayerBeneficialBuff()
    {
        PlayerData player = TrainingBattleManager.Instance != null ? TrainingBattleManager.Instance.playerData : null;
        if (player == null)
        {
            return false;
        }

        Buff stolenBuff = BuffCombatUtility.RemoveRandomBeneficialBuff(player);
        if (stolenBuff == null)
        {
            return false;
        }

        AddBuff(stolenBuff.data.buffId, Mathf.Max(1, stolenBuff.stack));
        TrainingBattleManager.Instance?.UpdateAllUI();
        return true;
    }

    private int GetPatternIdForCurrentRoute()
    {
        if (routeStage == 0)
        {
            return 30201;
        }

        if (stealSucceededLastCycle)
        {
            return routeStage == 1 ? 30202 : 30203;
        }

        return routeStage == 1 ? 30204 : 30205;
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return PreviewOutgoingDamage(baseDamage);
    }

    private void DealFixedDamage(PlayerData target, int damage)
    {
        if (target == null || damage <= 0)
        {
            return;
        }

        target.TakeDamage(damage, this);
    }
}
