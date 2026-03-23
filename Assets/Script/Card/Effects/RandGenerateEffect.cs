using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 랜덤 카드 생성 이펙트를 지정한 위치로 수행한다.
/// </summary>
public class RandGenerateEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<RandomCardData> RandomCard;
    public List<int> cardIdList;
    public MoveZoneType to;
    public MovePositionType position;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int finalAmount = ResolveAmount(battleManager);
        if (finalAmount <= 0)
        {
            return;
        }

        GenerateCardEffect generator = CreateGenerateEffect(to);
        if (generator == null)
        {
            return;
        }

        List<Card> generatedCards = new();
        for (int i = 0; i < finalAmount; i++)
        {
            if (generator.TryCreateCard(battleManager, out Card generatedCard))
            {
                generatedCards.Add(generatedCard);
            }
        }

        if (generatedCards.Count == 0)
        {
            return;
        }

        generatedCards = battleManager.ProcessGeneratedCards(generatedCards);
        if (generator.target == TargetType.Hand || generator.target == TargetType.None)
        {
            if (battleManager.handManager != null)
            {
                battleManager.handManager.AddCard(generatedCards);
            }
        }
        else
        {
            foreach (Card generatedCard in generatedCards)
            {
                generator.AddGeneratedCardToTarget(battleManager, generatedCard);
            }
        }

        battleManager.UpdateAllUI();
    }

    private int ResolveAmount(TrainingBattleManager battleManager)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        }

        return amount > 0 ? amount : 1;
    }

    private GenerateCardEffect CreateGenerateEffect(MoveZoneType to)
    {
        if ((RandomCard == null || RandomCard.Count == 0) && (cardIdList == null || cardIdList.Count == 0))
        {
            Debug.LogWarning("[RandGenerateEffect] 생성할 카드 풀이 없어 이펙트를 실행하지 않습니다.");
            return null;
        }

        return new GenerateCardEffect
        {
            RandomCard = RandomCard,
            cardIdList = cardIdList,
            target = ResolveTarget(to),
            position = position
        };
    }

    private static TargetType ResolveTarget(MoveZoneType to)
    {
        switch (to)
        {
            case MoveZoneType.DiscardPile:
                return TargetType.Discard;
            case MoveZoneType.DrawPile:
                return TargetType.Deck;
            case MoveZoneType.Hand:
                return TargetType.Hand;
            default:
                return TargetType.Hand;
        }
    }
}
