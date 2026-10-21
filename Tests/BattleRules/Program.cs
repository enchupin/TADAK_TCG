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

        PlayerData energyPlayer = new PlayerData { energy = 5, maxEnergy = 5 };
        energyPlayer.AddEnergy(2);
        Check(energyPlayer.energy == 7, "카드·버프로 얻는 에너지는 최대치 초과 가능");
        energyPlayer.AddEnergy(-3);
        Check(energyPlayer.energy == 7, "0 이하 획득량은 무시");

        CheckIdentity();
        CheckDeckReward();
        CheckRewardSequence();
        CheckCuriosity();

        Console.WriteLine($"전투 규칙 검증 {count}개 통과");
    }

    static CharacterIdentityService NewIdentityService()
    {
        CharacterManager.Characters.Clear();
        CharacterManager.Characters[Character.Isla] = new CharacterData { identity = new CharacterIdentityDefinition { cost = 6 } };
        CharacterManager.Characters[Character.Ignia] = new CharacterData { identity = new CharacterIdentityDefinition { cost = 10 } };
        CharacterManager.Characters[Character.Polar] = new CharacterData { identity = new CharacterIdentityDefinition { cost = 8 } };
        CharacterIdentityService service = new CharacterIdentityService(new TrainingBattleManager());
        service.Initialize(new List<Character> { Character.Polar, Character.Isla, Character.Ignia });
        return service;
    }

    static void CheckIdentity()
    {
        CharacterIdentityService service = NewIdentityService();
        Check(service.GetStates().Select(state => state.Character).SequenceEqual(new[] { Character.Isla, Character.Ignia, Character.Polar }), "아이덴티티 슬롯은 캐릭터 ID 순");
        Check(service.GetCost(Character.Isla) == 6 && service.GetCost(Character.Ignia) == 10 && service.GetCost(Character.Polar) == 8, "아이덴티티 비용 데이터 사용");

        service.ChargeForPlayedCard(new Card { character = Character.Ignia }, 2);
        service.ChargeForPlayedCard(new Card { character = Character.Ignia }, 0);
        service.ChargeForPlayedCard(new Card { character = Character.Ignia, costType = CardCostType.Barrier }, 5);
        Check(service.GetGauge(Character.Ignia) == 2, "지불한 에너지만 충전, 비용 0과 보호막 비용은 충전 없음");
        service.ChargeForPlayedCard(new Card { character = Character.Monster }, 3);
        Check(service.GetGauge(Character.Ignia) == 2 && service.GetGauge(Character.Isla) == 0, "몬스터 카드는 충전 없음");
        service.ChargeForPlayedCard(new Card { character = Character.Isla }, 9);
        Check(service.GetGauge(Character.Isla) == 6 && service.CanUse(Character.Isla), "충전은 비용에서 멈추고 활성화");

        Check(service.TryUse(Character.Isla) && service.GetGauge(Character.Isla) == 0, "사용 후 충전 0으로 초기화");
        Check(service.TryClaimCardReward(Character.Isla), "전투 첫 사용은 카드 획득 보상");
        Check(!service.TryClaimCardReward(Character.Isla), "같은 전투에서 보상은 1회");
        Check(!service.TryClaimCardReward(Character.Ignia), "사용하지 않은 캐릭터는 보상 없음");
        service.AddGauge(Character.Isla, 6);
        Check(service.TryUse(Character.Isla) && !service.TryClaimCardReward(Character.Isla), "재사용은 가능하지만 보상 없음");

        GameRules.Current.combat.chargeMode = IdentityChargeModes.Card;
        service.ChargeForPlayedCard(new Card { character = Character.Polar, costType = CardCostType.Barrier }, 0);
        Check(service.GetGauge(Character.Polar) == 1, "card 모드는 사용 횟수로 충전");
        GameRules.Current.combat.chargeMode = IdentityChargeModes.Energy;

        service.AddGauge(Character.Ignia, 5);
        Dictionary<Character, int> saved = service.ExportGauges();
        CharacterIdentityService nextBattle = NewIdentityService();
        nextBattle.RestoreGauges(saved);
        Check(nextBattle.GetGauge(Character.Ignia) == 7 && nextBattle.GetGauge(Character.Polar) == 1, "다음 전투에 충전 진행도 유지");
        Check(!nextBattle.TryClaimCardReward(Character.Ignia), "새 전투는 보상 기록 초기화, 사용 전에는 보상 없음");
        nextBattle.AddGauge(Character.Ignia, 3);
        Check(nextBattle.TryUse(Character.Ignia) && nextBattle.TryClaimCardReward(Character.Ignia), "새 전투 첫 사용 보상");
        nextBattle.RestoreGauges(new Dictionary<Character, int> { { Character.Polar, 99 } });
        Check(nextBattle.GetGauge(Character.Polar) == 8, "복원 값은 비용으로 제한");
    }

    // 중첩 IEnumerator와 WaitUntil을 처리하는 간단한 코루틴 실행기
    static void RunCoroutine(System.Collections.IEnumerator routine)
    {
        Stack<System.Collections.IEnumerator> stack = new Stack<System.Collections.IEnumerator>();
        stack.Push(routine);
        int guard = 0;
        while (stack.Count > 0)
        {
            Check(++guard < 10000, "보상 코루틴이 끝나지 않음");
            count--;
            System.Collections.IEnumerator current = stack.Peek();
            if (!current.MoveNext()) { stack.Pop(); continue; }
            if (current.Current is System.Collections.IEnumerator nested) stack.Push(nested);
            else if (current.Current is UnityEngine.WaitUntil wait) Check(wait.Predicate(), "선택 완료 전 대기 해제");
        }
    }

    static void CheckRewardSequence()
    {
        TrainingBattleManager manager = NewBattle(5, 0);
        TrainingBattleManager.buildingDeck = new BuildingDeck();
        for (int i = 0; i < 6; i++) TrainingBattleManager.buildingDeck.Cards.Add(new Card { cardId = 201010, character = Character.Ignia });
        RunCoroutine(manager.Reward(Character.Ignia));
        Check(!manager.IsDeckRewardInProgress, "보상 완료 후 진행 플래그 해제");
        Check(manager.RefreshFlags.Count > 0 && !manager.RefreshFlags[manager.RefreshFlags.Count - 1], "보상 완료 후 플래그 해제 상태로 손패 사용 가능 여부를 다시 계산");
        Check(manager.PanelOpenCount == 1 && TrainingBattleManager.buildingDeck.Cards.Count == 7, "3지선다 1회 후 저장 덱에 1장 추가");
        Card rewardCard = TrainingBattleManager.buildingDeck.Cards[6];
        Check(manager.usableDeckManager.Discard.Count == 1 && manager.usableDeckManager.Discard[0].cardId == rewardCard.cardId, "보상 카드는 이번 전투 버린 카드 더미에도 추가");
        Check(!ReferenceEquals(manager.usableDeckManager.Discard[0], rewardCard), "전투용 카드는 저장 덱과 별도 인스턴스");

        manager = NewBattle(5, 0);
        TrainingBattleManager.buildingDeck = new BuildingDeck();
        for (int i = 0; i < 8; i++) TrainingBattleManager.buildingDeck.Cards.Add(new Card { cardId = 201010, character = Character.Ignia });
        Card keptReward = null;
        manager.PickSelection = (cards, selectCount) => selectCount == 1 && cards.All(card => card.cardId % 1000 >= 30) ? cards.GetRange(0, 1) : cards.GetRange(0, selectCount);
        RunCoroutine(manager.Reward(Character.Ignia));
        keptReward = TrainingBattleManager.buildingDeck.Cards.FirstOrDefault(card => card.cardId != 201010);
        Check(manager.PanelOpenCount == 2, "8장 초과 시 제거 선택 창이 이어서 열림");
        Check(TrainingBattleManager.buildingDeck.Cards.Count(card => card.character == Character.Ignia) == 8 && keptReward != null, "넘은 1장을 제거해 캐릭터당 8장 유지");
        Check(!manager.RefreshFlags[manager.RefreshFlags.Count - 1], "초과 제거까지 마친 뒤 손패 상태 갱신");

        manager = NewBattle(5, 0);
        manager.PanelAvailable = false;
        TrainingBattleManager.buildingDeck = new BuildingDeck();
        RunCoroutine(manager.Reward(Character.Ignia));
        Check(TrainingBattleManager.buildingDeck.Cards.Count == 1 && !manager.IsDeckRewardInProgress, "선택 창이 없으면 첫 선택지로 진행하고 잠금 해제");
        TrainingBattleManager.buildingDeck = null;
    }

    static void CheckCuriosity()
    {
        TrainingBattleManager manager = NewBattle(0, 0);
        TrainingBattleManager.buildingDeck = new BuildingDeck();
        for (int i = 0; i < 4; i++)
        {
            Card deckCard = new Card { cardId = 201030 + i * 10, cost = 2, enforceCardIds = new List<int> { 201031 + i * 10, 201032 + i * 10, 201033 + i * 10, 201034 + i * 10, 201035 + i * 10 } };
            TrainingBattleManager.buildingDeck.Cards.Add(deckCard);
            manager.usableDeckManager.Draw.Add(new Card { cardId = deckCard.cardId, cost = 2, enforceCardIds = new List<int>(deckCard.enforceCardIds), sourceDeckIndex = i });
        }
        manager.usableDeckManager.Draw.Add(new Card { cardId = 201010, cost = 1, sourceDeckIndex = 9 });
        manager.usableDeckManager.Draw.Add(new Card { cardId = 201040, cost = 2, enforceCardIds = new List<int> { 201041 } });

        manager.StartCuriosity();
        List<Card> curious = manager.usableDeckManager.Draw.Where(card => card.isCurious).ToList();
        Check(curious.Count == 1, "전투당 호기심 1장");
        Check(curious[0].sourceDeckIndex >= 0 && curious[0].enforceCardIds.Count > 0, "강화 계열이 있는 저장 덱 카드만 호기심 대상");
        Check(curious[0].cost == 0, "호기심 카드 비용 0");

        Card target = curious[0];
        int deckIndex = target.sourceDeckIndex;
        List<int> upgradeLine = new List<int>(target.enforceCardIds);
        List<int> offered = null;
        manager.PickSelection = (cards, selectCount) => { offered = cards.Select(card => card.cardId).ToList(); return cards.GetRange(cards.Count - 1, 1); };
        RunCoroutine(manager.PlayCuriositySequence(target));
        Check(offered != null && offered.Count == 3 && offered.Distinct().Count() == 3, "강화 계열 5종 중 서로 다른 3종 제시");
        Check(offered.All(upgradeLine.Contains), "선택지는 해당 카드의 강화 계열");
        Check(target.cardId == offered.Last() && !target.isCurious && target.resolvedAsCuriosity, "고른 강화가 이번 전투 카드에 즉시 적용");
        Check(TrainingBattleManager.buildingDeck.Cards[deckIndex].cardId == offered.Last(), "고른 강화가 저장 덱에 영구 반영");

        BossModeSession.IsActive = true;
        manager = NewBattle(0, 0);
        manager.usableDeckManager.Draw.Add(new Card { cardId = 201030, enforceCardIds = new List<int> { 201031 }, sourceDeckIndex = 0 });
        manager.StartCuriosity();
        Check(!manager.usableDeckManager.Draw[0].isCurious, "보스전에는 호기심 없음");
        BossModeSession.IsActive = false;
        TrainingBattleManager.buildingDeck = null;
    }

    static void CheckDeckReward()
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 8; i++) deck.Add(new Card { cardId = 201010, character = Character.Ignia });
        deck.Add(new Card { cardId = 101010, character = Character.Isla });
        Check(TrainingDeckRewardService.GetOverflowCount(deck, Character.Ignia) == 0, "캐릭터당 8장은 초과 아님");
        deck.Add(new Card { cardId = 201030, character = Character.Ignia });
        Check(TrainingDeckRewardService.GetOverflowCount(deck, Character.Ignia) == 1 && TrainingDeckRewardService.GetOverflowCount(deck, Character.Isla) == 0, "9장째에 1장 초과");
        Check(TrainingDeckRewardService.GetCharacterCards(deck, Character.Ignia).Count == 9, "캐릭터 카드 목록");

        List<int> options = TrainingDeckRewardService.GetAcquisitionOptions(deck, Character.Ignia);
        Check(options.Count == 3 && options.Distinct().Count() == 3 && options.All(id => id / 1000 == 201 && id % 10 == 0 && id % 1000 >= 30), "고유 카드 기본형 중 서로 다른 3종");

        CardManager.UniqueCardIds.Add(201070);
        CardManager.UniqueCardIds.Add(201060);
        deck.Add(new Card { cardId = 201070, character = Character.Ignia });
        for (int i = 0; i < 20; i++)
        {
            Check(!TrainingDeckRewardService.GetAcquisitionOptions(deck, Character.Ignia).Contains(201070), "보유 중인 유일 카드는 선택지 제외");
        }
        Check(TrainingDeckRewardService.GetAcquisitionOptions(deck, Character.Ignia).Count == 3, "남은 후보 4종 중 3종 제시");
        Check(Enumerable.Range(0, 30).Any(_ => TrainingDeckRewardService.GetAcquisitionOptions(deck, Character.Ignia).Contains(201060)), "보유하지 않은 유일 카드는 후보에 포함");
        CardManager.UniqueCardIds.Clear();
    }
}
