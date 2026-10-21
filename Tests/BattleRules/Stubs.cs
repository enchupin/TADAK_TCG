using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
    }
}

// 테스트에서는 rules.json을 System.Text.Json으로 읽어 실제 데이터와 필드 매핑을 함께 검증
public static class GameRulesJsonParser
{
    public static string JsonPath;

    public static GameRulesData LoadOrDefault()
    {
        string json = File.ReadAllText(JsonPath);
        return JsonSerializer.Deserialize<GameRulesData>(json, new JsonSerializerOptions { IncludeFields = true });
    }
}

public class Card
{
    public int cardId;
    public string cardName = string.Empty;
    public int OnDrawCount;
    public void ExecuteOnDrawEffects(TrainingBattleManager manager) => OnDrawCount++;
}

public class BattleContext
{
    public int Drawn;
    public void OnCardsDrawn(int count) => Drawn += count;
}

public class UsableDeckManager
{
    public List<Card> Draw = new List<Card>();
    public List<Card> Discard = new List<Card>();
    public Card DrawCard() { Card card = Draw.FirstOrDefault(); if (card != null) Draw.Remove(card); return card; }
    public void AddToDiscard(Card card) => Discard.Add(card);
}

public partial class HandManager
{
    public readonly List<Card> handCardList = new List<Card>();
    public int GetHandCount() => handCardList.Count;
    public List<Card> GetHandCards() => new List<Card>(handCardList);
    public object AddCard(Card card, bool animateDraw) { handCardList.Add(card); return null; }
    public bool TryAdd(Card card) => TryRouteOverflowToDiscard(card);
}

public partial class TrainingBattleManager
{
    public static TrainingBattleManager Instance;
    public HandManager handManager = new HandManager();
    public UsableDeckManager usableDeckManager = new UsableDeckManager();
    public BattleContext battleContext = new BattleContext();
    public bool DrawAllowed = true;
    public bool overrideTurnSettingsFromInspector;
    public int drawCardCount = 6;
    public int maxHandCardCount = 10;
    public int playerBaseEnergyPerTurn = 3;
    public bool CanDrawCards() => DrawAllowed;
    public void RefreshHandPlayableState() { }
    public void UpdateAllUI() { }
    public List<Card> Draw(int count) => DrawCardsSequentially(count, false, () => usableDeckManager.DrawCard());
}
