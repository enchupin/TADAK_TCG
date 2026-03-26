using System.Collections.Generic;
using UnityEngine;

public class WoodenPuppetMonster : Monster
{
    private int plannedPatternIdForTurn;

    public override int MonsterId => 116;
    protected override string MonsterName => "목각 인형";
    protected override int BaseMaxHp => 55;

    protected override void BuildNextAction()
    {
        List<int> candidates = new List<int> { 11601, 11602 };
        if (CanUseDebuffPatterns())
        {
            candidates.Add(11603);
            candidates.Add(11604);
        }

        plannedPatternIdForTurn = candidates[Random.Range(0, candidates.Count)];
        switch (plannedPatternIdForTurn)
        {
            case 11601:
                SetAttackIntent(6, "피해를 6 입힙니다. 버린 카드 더미에 뿌리 흡수를 2장 생성합니다.");
                SetPlannedPattern(11601, MonsterIntentIconType.Attack, MonsterIntentIconType.DisruptCard);
                break;
            case 11602:
                SetAttackIntent(9, "피해를 9 입힙니다. 보호막을 8 얻습니다.");
                SetPlannedPattern(11602, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection);
                break;
            case 11603:
                SetIntent("빈약을 3 부여합니다.");
                SetPlannedPattern(11603, MonsterIntentIconType.HarmfulEffect);
                break;
            default:
                SetIntent("약화를 3 부여합니다.");
                SetPlannedPattern(11604, MonsterIntentIconType.HarmfulEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (plannedPatternIdForTurn)
        {
            case 11601:
                DealDamage(target, 6);
                AddRootAbsorptionCards(2);
                break;
            case 11602:
                DealDamage(target, 9);
                AddDefense(8);
                break;
            case 11603:
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 3);
                break;
            case 11604:
                target?.AddBuff(BattleRuntimeDefinitions.WeakBuffId, 3);
                break;
        }
    }

    private void AddRootAbsorptionCards(int count)
    {
        for (int cardIndex = 0; cardIndex < count; cardIndex++)
        {
            AddCardToPlayerDiscard(CardManager.GetCardAsCard(BattleRuntimeDefinitions.RootAbsorptionCardId));
        }
    }

    private bool CanUseDebuffPatterns()
    {
        PlayerData playerData = TrainingBattleManager.Instance?.playerData;
        if (playerData == null)
        {
            return false;
        }

        if (playerData.GetBuffStack(BattleRuntimeDefinitions.FrailBuffId) > 0
            || playerData.GetBuffStack(BattleRuntimeDefinitions.WeakBuffId) > 0)
        {
            return false;
        }

        List<Monster> livingMonsters = TrainingBattleManager.Instance.GetLivingMonsters();
        foreach (Monster monster in livingMonsters)
        {
            if (monster == null || monster == this || monster.IsDead() || monster.MonsterId != MonsterId)
            {
                continue;
            }

            if (monster.PlannedPatternId == 11603 || monster.PlannedPatternId == 11604)
            {
                return false;
            }
        }

        return true;
    }
}
