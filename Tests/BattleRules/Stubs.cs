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
        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
        public static int Min(int a, int b) => Math.Min(a, b);
    }

    public static class Random
    {
        public static System.Random Source = new System.Random(7);
        public static int Range(int min, int maxExclusive) => Source.Next(min, maxExclusive);
    }

    public class ScriptableObject { }

    public class WaitUntil
    {
        public readonly Func<bool> Predicate;
        public WaitUntil(Func<bool> predicate) { Predicate = predicate; }
    }
}

public static class BossModeSession { public static bool IsActive; }
public static class TrainingRunState { public static bool IsRunActive = true; }

public static class LocalizationManager
{
    public static int Applied;
    public static void ApplyToRuntimeCard(Card card) => Applied++;
}

public class BuildingDeck
{
    public readonly List<Card> Cards = new List<Card>();
    public IReadOnlyList<Card> GetCards() => Cards;
    public bool AddCard(int cardId) { Cards.Add(CardManager.GetCardAsCard(cardId)); return true; }
    public bool ReplaceCardAt(int index, int cardId) { Cards[index] = CardManager.GetCardAsCard(cardId); return true; }
    public void RemoveCard(Card card) => Cards.Remove(card);
}

public enum CardCostType { Energy, Barrier, Rune }

public class CharacterData : UnityEngine.ScriptableObject
{
    public CharacterIdentityDefinition identity = new CharacterIdentityDefinition();
}

public static class CharacterManager
{
    public static readonly Dictionary<Character, CharacterData> Characters = new Dictionary<Character, CharacterData>();
    public static CharacterData GetCharacterByEnum(Character character) => Characters.TryGetValue(character, out CharacterData data) ? data : null;
    public static List<int> GetUniqueBaseCardIds(Character character) => new List<int> { (int)character * 1000 + 30, (int)character * 1000 + 40, (int)character * 1000 + 50, (int)character * 1000 + 60, (int)character * 1000 + 70 };
}

public class CardData
{
    public int cardId;
    public List<int> keywords = new List<int>();
}

public static class CardManager
{
    public static readonly HashSet<int> UniqueCardIds = new HashSet<int>();
    public static Card GetCardAsCard(int cardId) => new Card { cardId = cardId, character = (Character)(cardId / 1000) };
    public static CardData GetCard(int cardId)
    {
        CardData data = new CardData { cardId = cardId };
        if (UniqueCardIds.Contains(cardId)) data.keywords.Add(CardKeywordIds.Unique);
        return data;
    }
}

public abstract class CharacterIdentitySkill
{
    public abstract bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState);
}

public sealed class TestIdentitySkill : CharacterIdentitySkill
{
    public int Executed;
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState) { Executed++; return true; }
}

public static class CharacterIdentitySkillRegistry
{
    public static Dictionary<Character, CharacterIdentitySkill> Create() => new Dictionary<Character, CharacterIdentitySkill>
    {
        { Character.Isla, new TestIdentitySkill() },
        { Character.Ignia, new TestIdentitySkill() },
        { Character.Polar, new TestIdentitySkill() }
    };
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
    public Character character;
    public CardCostType costType = CardCostType.Energy;
    public int cost = 1;
    public bool isCurious;
    public bool resolvedAsCuriosity;
    public int sourceDeckIndex = -1;
    public List<int> enforceCardIds = new List<int>();
    public void SetCost(int value, bool turnOnly) => cost = value;
    public void ApplyTemplate(Card template) { cardId = template.cardId; cost = template.cost; enforceCardIds = new List<int>(template.enforceCardIds); }
    public int OnDrawCount;
    public void ExecuteOnDrawEffects(TrainingBattleManager manager) => OnDrawCount++;
}

public partial class PlayerData
{
    public int energy;
    public int maxEnergy;
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
    public List<Card> GetDrawPile() => Draw;
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
    public static BuildingDeck buildingDeck;
    private bool isDeckRewardInProgress;
    public bool IsDeckRewardInProgress => isDeckRewardInProgress;
    public bool HasPendingSelection;
    public bool PanelAvailable = true;
    public int PanelOpenCount;
    public readonly List<bool> RefreshFlags = new List<bool>();
    public Func<List<Card>, int, List<Card>> PickSelection = (cards, selectCount) => cards.GetRange(0, selectCount);
    public void RefreshHandPlayableState() => RefreshFlags.Add(isDeckRewardInProgress);
    public void UpdateEndTurnButtonState() { }
    public bool OpenSelectCardPanel(List<Card> cards, int selectCount, Action<List<Card>> onSelected, bool allowFewer = false)
    {
        if (!PanelAvailable) return false;
        PanelOpenCount++;
        onSelected(PickSelection(cards, selectCount));
        return true;
    }
    public System.Collections.IEnumerator Reward(Character character) => RunIdentityCardRewardSequence(character);
    public void StartCuriosity() => ApplyCuriosityAtCombatStart();
    public void UpdateAllUI() { }
    public List<Card> Draw(int count) => DrawCardsSequentially(count, false, () => usableDeckManager.DrawCard());
}
