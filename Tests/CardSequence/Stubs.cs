using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 실제 효과와 실행기를 검증하기 위한 Unity 및 전투 환경 대역
namespace UnityEngine
{
    public class Coroutine { }
    public static class Debug
    {
        public static void Log(object value) { }
        public static void LogWarning(object value) { }
    }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Clamp(int n, int min, int max) => Math.Clamp(n, min, max);
        public static int Abs(int n) => Math.Abs(n);
    }
    public static class Random { public static int Range(int min, int max) => min; }
}

public enum BattleTurnState { PlayerAction, CombatEnd }
public class Monster { public bool IsDead() => false; }
public class CardUI { public Card Card; }
public class CardController
{
    public Card Card;
    public CardUI cardUI;
    public void PrepareAcceptedPlayAnimationStart() { }
}
public class CardPlayEventData
{
    public CardController cardController;
    public Monster targetMonster;
    public bool Accepted;
    public void MarkAccepted() => Accepted = true;
}
public class CardData
{
    public int cardId;
    public Character character;
    public Card ToCard() => new Card { cardId = cardId, character = character };
}
public static class CardManager
{
    public static readonly Dictionary<int, Card> Cards = new();
    public static Card GetCardAsCard(int id) => Cards.TryGetValue(id, out var c) ? c : null;
    public static CardData GetCard(int id) => null;
    public static List<CardData> GetCardsByCharacter(Character c) => new();
    public static List<CardData> GetAllCards() => new();
}
public static class FormulaEvaluator
{
    public static int Evaluate(string formula, BattleContext context, object player, int amount = 0) => int.Parse(formula);
}
public class ConditionData { public List<int> checks = new(); }
public static class ConditionEvaluator { public static bool Evaluate(ConditionData data, TrainingBattleManager manager) => true; }
public enum TargetType { None, Hand, Discard, Deck }
public class RandomCardData { }
public class GenerateCardEffect
{
    public List<RandomCardData> RandomCard;
    public List<int> cardIdList;
    public TargetType target;
    public MovePositionType position;
    public bool TryCreateCard(TrainingBattleManager manager, out Card card)
    {
        card = new Card { cardId = cardIdList[0] };
        manager.Generated++;
        return true;
    }
    public void AddGeneratedCardToTarget(TrainingBattleManager manager, Card card) => manager.usableDeckManager.AddToDiscard(card);
}
public class HandManager
{
    public List<Card> Cards = new();
    public const int Capacity = 10;
    public List<Card> GetHandCards() => new(Cards);
    public void AddCard(Card card) { if (Cards.Count < Capacity) Cards.Add(card); }
    public void AddCard(List<Card> cards) { foreach (var card in cards) AddCard(card); }
    public bool RemoveCard(Card card) => Cards.Remove(card);
    public UnityEngine.Coroutine RemoveCardFromHandWithUseAnimation(CardUI ui) { Cards.Remove(ui.Card); return null; }
}
public class UsableDeckManager
{
    public List<Card> Draw = new(), Discard = new(), Exhaust = new();
    public List<Card> GetDrawPile() => Draw;
    public List<Card> GetDiscardPile() => Discard;
    public List<Card> GetExhaustPile() => Exhaust;
    public void AddToDiscard(Card card) => Discard.Add(card);
    public void AddToDrawPileTop(Card card) => Draw.Insert(0, card);
    public void AddToDrawPileRandom(Card card) => Draw.Add(card);
    public bool RemoveFromDrawPile(Card card) => Draw.Remove(card);
    public void RemoveFromDiscard(Card card) => Discard.Remove(card);
    public Card DrawCard() { var c = Draw.FirstOrDefault(); if (c != null) Draw.Remove(c); return c; }
}
public class TrainingBattleManager
{
    public BattleContext battleContext = new();
    public HandManager handManager = new();
    public UsableDeckManager usableDeckManager = new();
    public object playerData = new();
    public Monster currentTarget;
    public BattleTurnState CurrentTurnState;
    public bool HasPendingSelection;
    public bool PanelAvailable = true, ImmediateSelection;
    public int OpenCount, RequiredCount, DrawCalls, Generated, Repeats, PowerCalls;
    public bool Ended;
    public List<Card> Candidates;
    private Action<List<Card>> callback;
    private readonly List<IEnumerator> routines = new();
    public UnityEngine.Coroutine StartCoroutine(IEnumerator sequence)
    {
        if (sequence.MoveNext()) routines.Add(sequence);
        return null;
    }
    public void Tick()
    {
        foreach (var routine in routines.ToArray())
            if (!routine.MoveNext()) routines.Remove(routine);
    }
    public bool OpenSelectCardPanel(List<Card> cards, int count, Action<List<Card>> action, bool allowFewer = false)
    {
        if (!PanelAvailable) return false;
        if (HasPendingSelection) throw new Exception("선택창 중복 실행");
        Candidates = cards; RequiredCount = count; callback = action; OpenCount++;
        HasPendingSelection = true;
        if (ImmediateSelection) Confirm(cards.Take(count).ToList());
        return true;
    }
    public void Confirm(List<Card> cards = null)
    {
        var action = callback;
        callback = null; HasPendingSelection = false;
        action(cards ?? Candidates.Take(RequiredCount).ToList());
    }
    public List<Card> DrawCardsAndGet(int count)
    {
        DrawCalls++;
        var result = new List<Card>();
        while (count-- > 0 && handManager.Cards.Count < HandManager.Capacity)
        {
            var c = usableDeckManager.DrawCard(); if (c == null) break;
            handManager.AddCard(c); result.Add(c);
        }
        battleContext.OnCardsDrawn(result.Count);
        return result;
    }
    public List<Card> ProcessGeneratedCards(List<Card> cards) => cards;
    public bool CanGainCardsToHandFrom(MoveZoneType from, string subject) => true;
    public bool CanPlayerPlayCard() => !HasPendingSelection;
    public bool CanPlayCard(Card card) => true;
    public bool CanPayCardCost(Card card) => true;
    public bool TryPayCardCost(Card card) => true;
    public int GetEffectiveCardCost(Card card) => card.cost;
    public int ConsumeRepeatedPlayCount(Card card, bool repeat) { int n = Repeats; Repeats = 0; return n; }
    public int GetCardUseAllEnemiesDamage() => 0;
    public void ApplyCardUseAllEnemiesDamage(int amount) { }
    public void HandlePlayedCardPowerEffects(Card card, Monster target, bool repeat) => PowerCalls++;
    public void ChargeIdentityGauge(Character character) { }
    public void RefreshHandPlayableState() { }
    public void UpdateAllUI() { }
    public void UpdateEndTurnButtonState() { }
    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card) => false;
    public bool ShouldExhaustUnlockedUnplayableCard(Card card) => false;
    public void MoveCardToExhaust(Card card) => usableDeckManager.Exhaust.Add(card);
    public void RemoveCardFromCombat(Card card) { }
    public bool TryHandleCombatEnd() => CurrentTurnState == BattleTurnState.CombatEnd;
    public void ForceEndPlayerTurn() => Ended = true;
    public List<Monster> GetLivingMonsters() => new();
}
