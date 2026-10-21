using System;
using System.Collections.Generic;

// IMPLEMENTATION.md 4절 조정 수치를 담는 데이터
// 기본값은 v1.0 수치이며 rules.json이 없거나 항목이 빠지면 기본값을 사용
[Serializable]
public sealed class GameRulesData
{
    public string version = "1.0";
    public CombatRulesData combat = new CombatRulesData();
    public DeckRulesData deck = new DeckRulesData();
    public IdentityRulesData identity = new IdentityRulesData();
    public TrainingRulesData training = new TrainingRulesData();
}

[Serializable]
public sealed class CombatRulesData
{
    // energy = 그 캐릭터 카드에 지불한 에너지, card = 카드 사용 횟수
    public string chargeMode = IdentityChargeModes.Energy;
    public int curiosityPerCombat = 1;
    public int drawPerTurn = 5;
    public int energyPerTurn = 5;
    public int handLimit = 10;
    public int maxEnemies = 4;
    public bool patternStagger = true;
    public bool stunConsumesIntent = true;
}

[Serializable]
public sealed class DeckRulesData
{
    public int choiceCount = 3;
    public int maxCardsPerCharacter = 8;
    public int partySize = 3;
    public int upgradesPerCard = 5;

    public int MaxDeckSize => Math.Max(0, maxCardsPerCharacter) * Math.Max(0, partySize);
}

[Serializable]
public sealed class IdentityRulesData
{
    public int usesPerNewCard = 1;
}

[Serializable]
public sealed class TrainingRulesData
{
    public int floors = 15;
    public int startFloor = 1;
    public List<int> exitFloors = new List<int> { 8, 15 };
    public float restHealRatio = 0.4f;
    public TrainingMapRulesData map = new TrainingMapRulesData();
}

[Serializable]
public sealed class TrainingMapRulesData
{
    public List<int> nodesPerFloor = new List<int> { 1, 2, 3, 3, 3, 3, 2, 1, 2, 3, 3, 3, 3, 2, 1 };
    public int maxOutDegree = 2;
    public int minNamedPerPath = 1;
    public int minRestPerPath = 2;
    public MapTypeWeightsData typeWeights = new MapTypeWeightsData();
    public List<MapSegmentData> segments = new List<MapSegmentData>
    {
        new MapSegmentData { tier = 1, fromFloor = 2, toFloor = 4 },
        new MapSegmentData { tier = 2, fromFloor = 5, toFloor = 7 },
        new MapSegmentData { tier = 3, fromFloor = 9, toFloor = 11 },
        new MapSegmentData { tier = 4, fromFloor = 12, toFloor = 14 }
    };
}

[Serializable]
public sealed class MapTypeWeightsData
{
    public float rest = 0.25f;
    public float normal = 0.55f;
    public float named = 0.2f;
}

[Serializable]
public sealed class MapSegmentData
{
    public int tier;
    public int fromFloor;
    public int toFloor;
}

public static class IdentityChargeModes
{
    public const string Energy = "energy";
    public const string Card = "card";
}

// 조정 수치 조회 진입점
public static class GameRules
{
    private static GameRulesData current;

    public static GameRulesData Current => current ??= GameRulesJsonParser.LoadOrDefault();

    public static CombatRulesData Combat => Current.combat ?? (Current.combat = new CombatRulesData());
    public static DeckRulesData Deck => Current.deck ?? (Current.deck = new DeckRulesData());
    public static IdentityRulesData Identity => Current.identity ?? (Current.identity = new IdentityRulesData());
    public static TrainingRulesData Training => Current.training ?? (Current.training = new TrainingRulesData());

    // 테스트나 에디터에서 다른 수치를 주입할 때 사용
    public static void Override(GameRulesData rules)
    {
        current = rules;
    }

    public static void Reload()
    {
        current = null;
    }
}
