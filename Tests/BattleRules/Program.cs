using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

static class Program
{
    static int count;

    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        count++;
    }

    static TrainingBattleManager NewBattle(int handCount, int deckCount)
    {
        TrainingBattleManager manager = new TrainingBattleManager();
        TrainingBattleManager.Instance = manager;
        for (int i = 0; i < handCount; i++) manager.handManager.handCardList.Add(new Card { cardId = 1000 + i });
        for (int i = 0; i < deckCount; i++) manager.usableDeckManager.Draw.Add(new Card { cardId = 2000 + i });
        return manager;
    }

    // rules.json의 모든 키가 데이터 클래스 필드에 연결되는지 재귀 확인
    static void CheckKeysMapped(JsonElement element, Type type, string path)
    {
        if (element.ValueKind != JsonValueKind.Object) return;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            FieldInfo field = type.GetField(property.Name, BindingFlags.Public | BindingFlags.Instance);
            Check(field != null, $"rules.json 키가 필드와 연결되지 않음: {path}{property.Name}");
            Type fieldType = field.FieldType;
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                CheckKeysMapped(property.Value, fieldType, path + property.Name + ".");
            }
            else if (property.Value.ValueKind == JsonValueKind.Array && fieldType.IsGenericType)
            {
                Type itemType = fieldType.GetGenericArguments()[0];
                foreach (JsonElement item in property.Value.EnumerateArray()) CheckKeysMapped(item, itemType, path + property.Name + "[].");
            }
        }
    }

    static void Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        string rulesPath = Path.Combine(root, "Assets", "Resources", "JsonData", "rules.json");
        GameRulesJsonParser.JsonPath = rulesPath;

        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(rulesPath)))
        {
            CheckKeysMapped(document.RootElement, typeof(GameRulesData), string.Empty);
        }

        GameRules.Reload();
        Check(GameRules.Combat.drawPerTurn == 5, "턴 시작 드로우 5");
        Check(GameRules.Combat.energyPerTurn == 5, "턴당 에너지 5");
        Check(GameRules.Combat.handLimit == 10, "손패 상한 10");
        Check(GameRules.Combat.maxEnemies == 4, "적 최대 4");
        Check(GameRules.Combat.chargeMode == IdentityChargeModes.Energy, "아이덴티티 충전 방식 energy");
        Check(GameRules.Combat.curiosityPerCombat == 1, "전투당 호기심 1");
        Check(GameRules.Combat.patternStagger && GameRules.Combat.stunConsumesIntent, "패턴 엇갈림과 기절 의도 소화");
        Check(GameRules.Deck.MaxDeckSize == 24 && GameRules.Deck.maxCardsPerCharacter == 8, "덱 최대 24장 캐릭터당 8장");
        Check(GameRules.Identity.usesPerNewCard == 1, "아이덴티티 카드 획득 1회");
        Check(GameRules.Training.map.nodesPerFloor.Count == GameRules.Training.floors, "층별 노드 수 목록이 층 수와 일치");
        Check(GameRules.Training.map.nodesPerFloor.Sum() == 35, "총 35개 노드");
        Check(GameRules.Training.exitFloors.SequenceEqual(new[] { 8, 15 }), "탈출구 8층과 15층");
        Check(Math.Abs(GameRules.Training.restHealRatio - 0.4f) < 0.0001f, "휴식 회복 40%");
        Check(GameRules.Training.map.segments.Count == 4 && GameRules.Training.map.segments[2].fromFloor == 9, "계층 4개와 3계층 시작 층");

        TrainingBattleManager manager = NewBattle(0, 0);
        Check(manager.DrawCardCount == 5 && manager.PlayerBaseEnergyPerTurn == 5 && manager.MaxHandCardCount == 10, "인스펙터 덮어쓰기 해제 시 규칙 수치를 사용");
        manager.overrideTurnSettingsFromInspector = true;
        manager.drawCardCount = 7;
        manager.maxHandCardCount = 8;
        manager.playerBaseEnergyPerTurn = 9;
        Check(manager.DrawCardCount == 7 && manager.MaxHandCardCount == 8 && manager.PlayerBaseEnergyPerTurn == 9, "인스펙터 덮어쓰기 체크 시 인스펙터 값을 사용");

        manager = NewBattle(8, 5);
        List<Card> drawn = manager.Draw(5);
        Check(drawn.Count == 2 && manager.handManager.GetHandCount() == 10, "손패가 차면 드로우 중단");
        Check(manager.usableDeckManager.Draw.Count == 3, "뽑지 못한 카드는 덱에 남음");
        Check(manager.battleContext.Drawn == 2 && drawn.All(card => card.OnDrawCount == 1), "실제로 뽑은 카드만 드로우 집계와 드로우 효과");

        manager = NewBattle(10, 3);
        Check(manager.Draw(2).Count == 0 && manager.usableDeckManager.Draw.Count == 3, "가득 찬 손패에서 턴 시작 드로우 없음");

        manager = NewBattle(9, 0);
        Card generated = new Card { cardId = 3000 };
        Check(!manager.handManager.TryAdd(generated), "손패에 여유가 있으면 손으로 이동");
        manager.handManager.handCardList.Add(generated);
        Card overflow = new Card { cardId = 3001 };
        Check(manager.handManager.TryAdd(overflow) && manager.usableDeckManager.Discard.Contains(overflow), "가득 찬 손패로 생성된 카드는 버린 카드 더미로");

        TrainingBattleManager.Instance = null;
        HandManager lobbyHand = new HandManager();
        for (int i = 0; i < 12; i++) lobbyHand.handCardList.Add(new Card());
        Check(!lobbyHand.TryAdd(new Card()), "전투 밖 손패는 상한 미적용");

        Console.WriteLine($"전투 규칙 검증 {count}개 통과");
    }
}
