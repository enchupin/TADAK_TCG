using System.Collections.Generic;

/// <summary>
/// SelectCard의 선택 후 동작(onAction)으로 선택 결과 판정
/// 고른 카드에 적용되지 않는 동작(예: 정해진 카드 생성)이나 알 수 없는 동작은 건너뜀
/// </summary>
public static class CardSelectionOutcomeResolver
{
    public static List<CardSelectionOutcome> FromSelectActions(MoveZoneType selectFrom, IEnumerable<ICardEffect> actions)
    {
        List<CardSelectionOutcome> outcomes = new List<CardSelectionOutcome>();
        Collect(selectFrom, actions, outcomes);
        return outcomes;
    }

    private static void Collect(MoveZoneType selectFrom, IEnumerable<ICardEffect> actions, List<CardSelectionOutcome> outcomes)
    {
        if (actions == null)
        {
            return;
        }

        foreach (ICardEffect action in actions)
        {
            switch (action)
            {
                case ConditionalEffect conditional:
                    // 조건과 관계없이 일어날 수 있는 결과를 모두 (같으면 한 번)
                    Collect(selectFrom, conditional.successEffects, outcomes);
                    Collect(selectFrom, conditional.failEffects, outcomes);
                    break;
                case MoveEffect move:
                    AddMove(move.to, move.position, outcomes);
                    break;
                case CopyEffect copy:
                    // 전투 중 카드 더미에서 고른 카드는 복제, 카드 풀(고유 카드 등)에서 고른 카드는 획득
                    Add(IsTemplatePool(selectFrom) ? CardSelectionOutcome.AddToHand : CardSelectionOutcome.CopyToHand, outcomes);
                    break;
                case ExhaustCardEffect:
                    Add(CardSelectionOutcome.Exhaust, outcomes);
                    break;
                case UpgradeCardsEffect:
                    Add(CardSelectionOutcome.Upgrade, outcomes);
                    break;
                case GenerateCardEffect:
                    Add(CardSelectionOutcome.AddToHand, outcomes);
                    break;
                case ReduceCostEffect:
                    Add(CardSelectionOutcome.ReduceCost, outcomes);
                    break;
                case TransformCardsEffect:
                    Add(CardSelectionOutcome.Transform, outcomes);
                    break;
            }
        }
    }

    private static void AddMove(MoveZoneType to, MovePositionType position, List<CardSelectionOutcome> outcomes)
    {
        switch (to)
        {
            case MoveZoneType.Hand:
                Add(CardSelectionOutcome.MoveToHand, outcomes);
                break;
            case MoveZoneType.DiscardPile:
                Add(CardSelectionOutcome.Discard, outcomes);
                break;
            case MoveZoneType.DrawPile when position == MovePositionType.Top:
                Add(CardSelectionOutcome.MoveToDrawTop, outcomes);
                break;
        }
    }

    private static bool IsTemplatePool(MoveZoneType from)
    {
        return from == MoveZoneType.Unique || from == MoveZoneType.AllUnique
            || from == MoveZoneType.Basic || from == MoveZoneType.CardId;
    }

    private static void Add(CardSelectionOutcome outcome, List<CardSelectionOutcome> outcomes)
    {
        if (!outcomes.Contains(outcome))
        {
            outcomes.Add(outcome);
        }
    }
}
