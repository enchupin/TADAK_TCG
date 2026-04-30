using System.Collections.Generic;
using UnityEngine;

public class MushroomHostMonster : Monster
{
    private static readonly int[] OpeningPatternSequence = { 20301, 20302, 20303, 20304, 20301, 20302, 20303 };
    private int actionCount;

    public override int MonsterId => 203;
    protected override string MonsterName => "버섯 숙주";
    protected override int BaseMaxHp => 180;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.ParasiticMushroomBuffId, 1);
    }

    protected override void BuildNextAction()
    {
        switch (GetCurrentPatternId())
        {
            case 20301:
                SetIntent($"적의 손에 {GetGeneratedPoisonCardName()} 카드를 2장 생성합니다.");
                SetPlannedPattern(20301, MonsterIntentIconType.DisruptCard);
                break;
            case 20302:
                SetAttackIntent(16, "피해를 16 입힙니다. 빈약을 2 부여합니다.");
                SetPlannedPattern(20302, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
                break;
            case 20303:
                SetAttackIntent(21, "피해를 21 입힙니다.");
                SetPlannedPattern(20303, MonsterIntentIconType.Attack);
                break;
            default:
                SetIntent("중독 강화를 얻습니다.");
                SetPlannedPattern(20304, MonsterIntentIconType.BeneficialEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (GetCurrentPatternId())
        {
            case 20301:
                AddPoisonCardsToPlayerHand(2);
                break;
            case 20302:
                DealDamage(target, 16);
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 2);
                break;
            case 20303:
                DealDamage(target, 21);
                break;
            default:
                TrainingBattleManager.Instance?.ApplyBuffToMonster(this, BattleRuntimeDefinitions.PoisonUpgradeBuffId, 1);
                break;
        }

        actionCount++;
    }

    private int GetCurrentPatternId()
    {
        if (actionCount < OpeningPatternSequence.Length)
        {
            return OpeningPatternSequence[actionCount];
        }

        int repeatIndex = (actionCount - OpeningPatternSequence.Length) % 3;
        return OpeningPatternSequence[repeatIndex];
    }

    private string GetGeneratedPoisonCardName()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        int resolvedCardId = battleManager != null
            ? battleManager.ResolvePersistentUpgradeCardId(20)
            : 20;
        Card resolvedCard = CardManager.GetCardAsCard(resolvedCardId);
        if (resolvedCard != null && !string.IsNullOrWhiteSpace(resolvedCard.cardName))
        {
            return resolvedCard.cardName;
        }

        Card baseCard = CardManager.GetCardAsCard(20);
        return baseCard != null ? baseCard.cardName : string.Empty;
    }

    private void AddPoisonCardsToPlayerHand(int count)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager == null || battleManager.handManager == null || !battleManager.CanGainCardsToHand())
        {
            return;
        }

        List<Card> generatedCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card generatedCard = CardManager.GetCardAsCard(20);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        List<Card> processedCards = battleManager.ProcessGeneratedCards(generatedCards, true);
        if (processedCards.Count == 0)
        {
            return;
        }

        battleManager.handManager.AddCard(processedCards);
        battleManager.UpdateAllUI();
    }

}
