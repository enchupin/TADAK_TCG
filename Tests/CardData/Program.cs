using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

// v1.0 카드 JSON 구조 검증: 효과 타입, 버프/키워드/그룹/카드 참조, 로컬라이징, 시작 덱
public static class Program
{
    // v1.0 데이터로 교체가 끝난 캐릭터 카드 파일 (단계별로 추가)
    private static readonly string[] MigratedCardFiles = { "isla", "jack", "cream", "ignia", "vanessa", "merel", "mio", "polar", "rune", "khan" };

    private static int passed;
    private static readonly List<string> failures = new List<string>();

    private static void Check(bool condition, string message)
    {
        if (condition) { passed++; return; }
        failures.Add(message);
    }

    private static JObject Load(string path)
    {
        return JObject.Parse(File.ReadAllText(path));
    }

    public static int Main(string[] args)
    {
        string root = args[0];
        string jsonDir = Path.Combine(root, "Assets/Resources/JsonData");
        string locDir = Path.Combine(root, "Assets/Resources/Localization");

        // 실제 변환기의 효과 타입 문자열
        string converter = File.ReadAllText(Path.Combine(root, "Assets/Editor/CardJSONConverter.cs"));
        // ParseEffectType 본문의 case 라벨 (한 반환을 공유하는 라벨 포함, 미지원 throw 라벨 제외)
        string parseBody = converter.Substring(converter.IndexOf("private static EffectType ParseEffectType"));
        parseBody = parseBody.Substring(0, parseBody.IndexOf("default:"));
        HashSet<string> effectTypes = new HashSet<string>(
            Regex.Matches(parseBody, "case \"(\\w+)\":(?!\\s*throw)").Select(m => m.Groups[1].Value));
        Check(effectTypes.Count > 20, "변환기 효과 타입 추출");

        HashSet<int> buffIds = new HashSet<int>(Load(Path.Combine(jsonDir, "buffs.json"))["buffs"].Select(b => (int)b["buffId"]));
        HashSet<int> keywordIds = new HashSet<int>(Load(Path.Combine(jsonDir, "keywords.json"))["keywords"].Select(k => (int)k["keywordId"]));
        Dictionary<string, List<int>> groups = Load(Path.Combine(jsonDir, "cardGroups.json"))["groups"]
            .ToDictionary(g => (string)g["groupName"], g => g["cardIds"].Select(id => (int)id).ToList());

        // 전체 카드 ID (미이전 캐릭터 포함, 참조 확인용)
        Dictionary<int, string> allCardIds = new Dictionary<int, string>();
        foreach (string file in Directory.GetFiles(jsonDir, "*Cards.json")) {
            foreach (JToken card in Load(file)["cards"]) {
                int id = (int)card["cardId"];
                Check(!allCardIds.ContainsKey(id), $"카드 ID 중복 {id}");
                allCardIds[id] = Path.GetFileName(file);
            }
        }

        foreach (KeyValuePair<string, List<int>> group in groups) {
            foreach (int id in group.Value) {
                Check(allCardIds.ContainsKey(id), $"그룹 {group.Key}의 카드 {id} 없음");
            }
        }

        foreach (string name in MigratedCardFiles) {
            JArray cards = (JArray)Load(Path.Combine(jsonDir, name + "Cards.json"))["cards"];
            // 한국어 표(ko/cards.json)는 JsonData에서 동기화로 생성
            JObject koCards = Load(Path.Combine(locDir, "ko", "cards.json"));
            Check(cards.Count > 0, $"{name} 카드 없음");

            foreach (JToken card in cards) {
                int id = (int)card["cardId"];
                int characterId = (int)card["characterId"];
                string tag = $"{name}:{id}";
                Check(id / 1000 == characterId, $"{tag} 캐릭터 ID 접두 불일치");
                Check(!string.IsNullOrWhiteSpace((string)card["name"]), $"{tag} 이름 없음");
                Check((string)koCards[$"card.{id}.name"] == (string)card["name"], $"{tag} 로컬라이징 이름 불일치");
                Check(string.IsNullOrEmpty((string)card["description"]) || !string.IsNullOrWhiteSpace((string)koCards[$"card.{id}.desc"]), $"{tag} 로컬라이징 설명 없음");

                foreach (JToken keyword in (JArray)card["keywords"] ?? new JArray()) {
                    Check(keywordIds.Contains((int)keyword), $"{tag} 키워드 {keyword} 없음");
                }

                string enforce = (string)card["enforceGroup"];
                if (enforce != null) {
                    Check(groups.ContainsKey(enforce), $"{tag} 강화 그룹 {enforce} 없음");
                }

                // 기본형(x0) 고유 카드는 강화 그룹이 있어야 함
                int group = id % 1000 / 10;
                if (group >= 3 && group <= 7 && id % 10 == 0) {
                    Check(enforce != null, $"{tag} 고유 카드 강화 그룹 누락");
                }

                List<JObject> effects = new List<JObject>();
                CollectEffects(card["effects"], effects);
                CollectEffects(card["onDrawEffects"], effects);
                CollectEffects(card["endTurnInHandEffects"], effects);
                bool unplayable = card["keywords"] is JArray kw && kw.Any(k => (int)k == 2);
                Check(effects.Count > 0 || unplayable, $"{tag} 효과 없음");

                foreach (JObject effect in effects) {
                    ValidateEffect(tag, effect, effectTypes, buffIds, groups, allCardIds);
                }

                // 변환기 구조 규칙 (onAction 바깥에서만 검사)
                ValidateConverterRules(tag, card["effects"], false);
                ValidateConverterRules(tag, card["onDrawEffects"], false);
                ValidateConverterRules(tag, card["endTurnInHandEffects"], false);

                string description = (string)card["description"];
                if (description.Contains("{attack}")) {
                    Check(effects.Any(e => (string)e["type"] == "Attack" || (string)e["type"] == "Damage"), $"{tag} {{attack}} 대상 효과 없음");
                }
                if (description.Contains("{block}")) {
                    Check(effects.Any(e => (string)e["type"] == "Barrier"), $"{tag} {{block}} 대상 효과 없음");
                }
            }
        }

        // 시작 덱 카드 존재
        foreach (JToken character in Load(Path.Combine(jsonDir, "characters.json"))["characters"]) {
            foreach (JToken id in (JArray)character["starterDeck"] ?? new JArray()) {
                Check(allCardIds.ContainsKey((int)id), $"캐릭터 {character["characterId"]} 시작 덱 카드 {id} 없음");
            }
        }

        foreach (string failure in failures) {
            Console.WriteLine("FAIL: " + failure);
        }
        Console.WriteLine($"통과 {passed}, 실패 {failures.Count}");
        return failures.Count == 0 ? 0 : 1;
    }

    // condition/onAction/Keep 하위 효과까지 평탄화
    private static void CollectEffects(JToken token, List<JObject> result)
    {
        if (token == null) return;
        if (token is JArray array) {
            foreach (JToken item in array) CollectEffects(item, result);
            return;
        }
        if (!(token is JObject effect)) return;

        result.Add(effect);
        if (effect["condition"] is JObject condition) {
            CollectEffects(condition["effects"], result);
            CollectEffects(condition["elseEffects"], result);
        }
        CollectEffects(effect["onAction"], result);
        CollectEffects(effect["effects"], result);
    }

    // CardJSONConverter.ReadEffect: onAction이 있으면 바깥 subject 필수, SelectCard는 바깥 from 필수
    private static void ValidateConverterRules(string tag, JToken token, bool inOnAction)
    {
        if (token is JArray array) {
            foreach (JToken item in array) ValidateConverterRules(tag, item, inOnAction);
            return;
        }
        if (!(token is JObject effect)) return;

        bool hasOnAction = effect["onAction"] is JObject || effect["onAction"] is JArray;
        if (!inOnAction) {
            Check(!hasOnAction || !string.IsNullOrWhiteSpace((string)effect["subject"]), $"{tag} onAction이 있는 {effect["type"]}에 subject 없음");
            if ((string)effect["type"] == "SelectCard") {
                Check(!string.IsNullOrWhiteSpace((string)effect["from"]), $"{tag} SelectCard에 from 없음");
            }
        }

        // 위치와 무관한 스키마 규칙 (Copy · Repeat · Kill · 이전 키워드)
        string type = (string)effect["type"];
        if (type == "Copy") {
            Check(effect["cardId"] == null && effect["target"] == null, $"{tag} Copy에 이전 필드 cardId/target");
            Check(!string.IsNullOrWhiteSpace((string)effect["from"]) && !string.IsNullOrWhiteSpace((string)effect["to"]), $"{tag} Copy에 from/to 없음");
            if ((string)effect["from"] == "CardId") {
                Check(effect["cardIds"] is JArray ids && ids.Count > 0, $"{tag} Copy CardId에 cardIds 없음");
            }
        }
        if (type == "Repeat") {
            Check(effect["amount"] != null || !string.IsNullOrWhiteSpace((string)effect["amountFormula"]), $"{tag} Repeat에 amount 없음");
            Check(effect["count"] == null && effect["effects"] == null && effect["onAction"] == null && effect["subject"] == null, $"{tag} Repeat에 지원하지 않는 필드");
        }
        if (type == "Kill") {
            Check(effect["amount"] == null && effect["amountFormula"] == null, $"{tag} Kill에 amount");
        }
        string formula = (string)effect["amountFormula"];
        Check(formula == null || formula.IndexOf("discarded", StringComparison.OrdinalIgnoreCase) < 0, $"{tag} 이전 수식 키워드 discarded");
        Check(!string.Equals((string)effect["subject"], "DiscardedCount", StringComparison.OrdinalIgnoreCase), $"{tag} 이전 subject DiscardedCount");

        if (effect["condition"] is JObject condition) {
            ValidateConverterRules(tag, condition["effects"], inOnAction);
            ValidateConverterRules(tag, condition["elseEffects"], inOnAction);
        }
        ValidateConverterRules(tag, effect["effects"], inOnAction);
        ValidateConverterRules(tag, effect["onAction"], true);
    }

    private static void ValidateEffect(string tag, JObject effect, HashSet<string> effectTypes, HashSet<int> buffIds,
        Dictionary<string, List<int>> groups, Dictionary<int, string> allCardIds)
    {
        string type = (string)effect["type"];
        if (type == null) {
            Check(effect["condition"] is JObject, $"{tag} 타입 없는 효과");
            foreach (JToken check in (JArray)effect["condition"]["checks"] ?? new JArray()) {
                string property = (string)check["property"];
                if ((property == "Buff" || property == "HasBuff") && int.TryParse((string)check["param"], out int checkBuff)) {
                    Check(buffIds.Contains(checkBuff), $"{tag} 조건 버프 {checkBuff} 없음");
                }
            }
            return;
        }

        Check(effectTypes.Contains(type), $"{tag} 변환기 미지원 효과 타입 {type}");

        if (effect["buffId"] != null) {
            Check(buffIds.Contains((int)effect["buffId"]), $"{tag} 버프 {effect["buffId"]} 없음");
        }
        foreach (JToken id in (JArray)effect["buffIds"] ?? new JArray()) {
            Check(buffIds.Contains((int)id), $"{tag} 버프 {id} 없음");
        }
        foreach (JToken id in (JArray)effect["cardIds"] ?? new JArray()) {
            Check(allCardIds.ContainsKey((int)id), $"{tag} 생성 카드 {id} 없음");
        }

        string cardIdGroup = (string)effect["cardIdGroup"];
        if (cardIdGroup != null) {
            Check(groups.ContainsKey(cardIdGroup), $"{tag} 카드 그룹 {cardIdGroup} 없음");
        }

        // 구역 이름이 아닌 from은 카드 그룹이어야 함
        string from = (string)effect["from"];
        if (from != null && !IsZone(from)) {
            Check(groups.ContainsKey(from), $"{tag} from 그룹 {from} 없음");
        }

        if (type == "GenerateCard" && int.TryParse((string)effect["cardId"], out int generatedId)) {
            Check(allCardIds.ContainsKey(generatedId), $"{tag} 생성 카드 {generatedId} 없음");
        }

        // 수식의 TargetBuff/Stack 버프 ID
        string formula = (string)effect["amountFormula"];
        if (formula != null) {
            foreach (Match match in Regex.Matches(formula, "(?:TargetBuff|TargetStack|GetBuffStack|Stack)\\((\\d+)\\)")) {
                Check(buffIds.Contains(int.Parse(match.Groups[1].Value)), $"{tag} 수식 버프 {match.Groups[1].Value} 없음");
            }
        }
    }

    private static bool IsZone(string value)
    {
        switch (value) {
            case "Source": case "Hand": case "Deck": case "DrawPile": case "Discard": case "DiscardPile":
            case "AllCards": case "CardId": case "Basic": case "Unique": case "AllUnique": case "Exhaust": case "ExhaustPile":
            case "Selected": case "SelectedCard": case "ThisCard": case "DrawnCards": case "GeneratedCard":
                return true;
            default:
                return false;
        }
    }
}
