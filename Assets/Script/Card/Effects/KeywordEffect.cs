using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 키워드 효과 (보존, 휘발, 소멸 등)
/// </summary>
public class KeywordEffect : ICardEffect
{
    public string keyword;  // "보존", "휘발", "소멸", "연쇄" 등
    public int amount;
    public string amountFormula;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int keywordAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        if (keywordAmount <= 0)
        {
            return;
        }

        int keywordId = ResolveKeywordId(keyword);
        if (keywordId <= 0)
        {
            Debug.LogWarning($"[KeywordEffect] 알 수 없는 키워드입니다: {keyword}");
            return;
        }

        List<Card> targetCards = ResolveTargetCards(battleManager);
        foreach (Card card in targetCards)
        {
            card?.AddKeyword(keywordId);
            battleManager.handManager?.RefreshCardDisplay(card);
        }

        battleManager.UpdateAllUI();
    }

    private static int ResolveKeywordId(string keywordName)
    {
        switch (keywordName)
        {
            case "보존": return CardKeywordIds.Keep;
            case "사용불가": return CardKeywordIds.Unplayable;
            case "소멸": return CardKeywordIds.Exhaust;
            case "파워": return CardKeywordIds.Power;
            case "개시": return CardKeywordIds.Opening;
            case "그림자": return CardKeywordIds.Shadow;
            case "종언": return CardKeywordIds.Finale;
            case "유령": return CardKeywordIds.Ghost;
            case "유일": return CardKeywordIds.Unique;
            default: return 0;
        }
    }

    private static List<Card> ResolveTargetCards(TrainingBattleManager battleManager)
    {
        List<Card> targetCards = new List<Card>();
        AddUnique(targetCards, battleManager?.battleContext?.GetContextCards("Source"));
        AddUnique(targetCards, battleManager?.battleContext?.GetContextCards("ThisCard"));
        AddUnique(targetCards, battleManager?.battleContext?.GetSelectedCards());
        AddUnique(targetCards, battleManager?.battleContext?.GetLastPlayedCard());
        return targetCards;
    }

    private static void AddUnique(List<Card> target, List<Card> source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (Card card in source)
        {
            AddUnique(target, card);
        }
    }

    private static void AddUnique(List<Card> target, Card card)
    {
        if (target == null || card == null || target.Contains(card))
        {
            return;
        }

        target.Add(card);
    }
}
