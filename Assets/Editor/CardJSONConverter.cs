using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// JSON card data to ScriptableObject converter.
/// </summary>
public class CardJSONConverter : EditorWindow
{
    private const string DefaultJsonFolderPath = "Assets/Resources/JsonData";
    private const string DefaultOutputPath = "Assets/Data/Cards";
    private const string CardCollectionPath = "Assets/Resources/CardCollection.asset";
    private const string CardIdGroupsFileName = "cardIdGroups.json";

    private string jsonFolderPath = DefaultJsonFolderPath;
    private string outputPath = DefaultOutputPath;
    private Dictionary<string, List<int>> cardIdGroups = new Dictionary<string, List<int>>();

    [MenuItem("Tools/TCG/Card JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CardJSONConverter>("Card Converter");
    }

    /// <summary>
    /// Batch-mode entry point for CI/local automation.
    /// Usage: Unity.exe -batchmode -quit -projectPath <path> -executeMethod CardJSONConverter.ConvertAllDefaultJsons
    /// </summary>
    public static void ConvertAllDefaultJsons()
    {
        CardJSONConverter converter = CreateInstance<CardJSONConverter>();
        converter.jsonFolderPath = DefaultJsonFolderPath;
        converter.outputPath = DefaultOutputPath;
        converter.ConvertJSONToScriptableObjects();
    }

    private void OnGUI()
    {
        GUILayout.Label("Card JSON Batch Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        jsonFolderPath = EditorGUILayout.TextField("JSON Folder Path", jsonFolderPath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        GUILayout.Space(10);
        if (GUILayout.Button("Convert All JSONs in Folder", GUILayout.Height(30)))
        {
            ConvertJSONToScriptableObjects();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Converts all '*Cards.json' files in the selected folder into CardData assets and updates CardCollection.",
            MessageType.Info);
    }

    private void ConvertJSONToScriptableObjects()
    {
        // 전체 변환 오케스트레이션 메서드.
        // 진행 순서:
        // 1) 입력 폴더/파일 검증
        // 2) 카드 그룹(cardIdGroups) 로드
        // 3) 기존 CardData 인덱싱
        // 4) 각 *Cards.json 파일 변환
        // 5) CardCollection 재구성/정렬/저장
        if (!Directory.Exists(jsonFolderPath))
        {
            Debug.LogError($"[CardJSONConverter] Folder not found: {jsonFolderPath}");
            EditorUtility.DisplayDialog("Error", "JSON folder path is invalid.", "OK");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*Cards.json");
        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"No '*Cards.json' found under {jsonFolderPath}", "OK");
            return;
        }

        EnsureDirectory(outputPath);
        cardIdGroups = LoadCardIdGroups();

        CardCollection collection = GetOrCreateCardCollection();
        Dictionary<int, CardData> existingById = BuildCardIndexById();
        Dictionary<int, CardData> updatedById = new Dictionary<int, CardData>();

        int converted = 0;
        foreach (string filePath in jsonFiles)
        {
            converted += ConvertFileInternal(filePath, existingById, updatedById);
        }

        List<CardData> normalized = new List<CardData>(updatedById.Values);
        normalized.Sort((a, b) => a.cardId.CompareTo(b.cardId));
        collection.allCards = normalized;

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            $"Converted {converted} cards from {jsonFiles.Length} files.\nCollection size: {collection.allCards.Count}",
            "OK");
    }

    private int ConvertFileInternal(string path, Dictionary<int, CardData> existingById, Dictionary<int, CardData> updatedById)
    {
        // 단일 JSON 파일을 읽어 CardData 에셋으로 반영한다.
        // existingById: 기존 에셋 재사용을 위한 인덱스
        // updatedById: 이번 변환 사이클에서 실제 갱신된 카드 집합
        string assetPath = path.Replace("\\", "/");
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"[CardJSONConverter] Could not load JSON asset: {assetPath}");
            return 0;
        }

        JObject root;
        try
        {
            root = JObject.Parse(jsonFile.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardJSONConverter] Invalid JSON in {assetPath}: {ex.Message}");
            return 0;
        }

        if (!(root["cards"] is JArray cardsArray))
        {
            Debug.LogWarning($"[CardJSONConverter] Missing 'cards' array in {assetPath}");
            return 0;
        }

        int count = 0;
        foreach (JToken token in cardsArray)
        {
            if (!(token is JObject cardObject))
            {
                continue;
            }

            int cardId = ReadInt(cardObject, "cardId");
            if (cardId <= 0)
            {
                Debug.LogWarning($"[CardJSONConverter] Skipped card without valid cardId in {assetPath}");
                continue;
            }

            CardData cardData = GetOrCreateCardData(cardObject, cardId, existingById);
            if (cardData == null)
            {
                continue;
            }

            UpdateCardData(cardData, cardObject);

            if (!AssetDatabase.Contains(cardData))
            {
                string newAssetPath = BuildCardAssetPath(cardData);
                AssetDatabase.CreateAsset(cardData, newAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(cardData);
            }

            existingById[cardId] = cardData;
            updatedById[cardId] = cardData;
            count++;
        }

        Debug.Log($"[CardJSONConverter] {Path.GetFileName(path)} converted: {count}");
        return count;
    }

    private CardData GetOrCreateCardData(JObject cardObject, int cardId, Dictionary<int, CardData> existingById)
    {
        // 카드 에셋 획득 전략:
        // 1) 메모리 인덱스(existingById)에서 우선 검색
        // 2) 예상 경로(candidatePath)에서 에셋 로드 시도
        // 3) 없으면 새 ScriptableObject 인스턴스 생성
        if (existingById.TryGetValue(cardId, out CardData existing) && existing != null)
        {
            return existing;
        }

        string candidatePath = BuildCardAssetPath(cardId, ReadString(cardObject, "name"));
        CardData loaded = AssetDatabase.LoadAssetAtPath<CardData>(candidatePath);
        if (loaded != null)
        {
            return loaded;
        }

        return ScriptableObject.CreateInstance<CardData>();
    }

    private void UpdateCardData(CardData cardData, JObject cardObject)
    {
        // JSON 카드 1건을 CardData에 매핑한다.
        // 중요 포인트:
        // - enforceGroup + enforce 배열을 모두 합쳐 enforceCardIds를 구성
        // - effects 배열은 ReadEffect로 재귀 파싱
        // - 기존 effects는 Clear 후 재구성하여 JSON을 단일 진실 원천으로 유지
        cardData.cardId = ReadInt(cardObject, "cardId");
        cardData.cardName = ReadString(cardObject, "name");
        cardData.character = CharacterManager.GetCharacterEnumById(ReadInt(cardObject, "characterId"));
        cardData.cost = ReadInt(cardObject, "cost");
        cardData.description = ReadString(cardObject, "description");

        cardData.enforceCardIds.Clear();
        string enforceGroup = ReadString(cardObject, "enforceGroup");
        if (!string.IsNullOrWhiteSpace(enforceGroup))
        {
            if (cardIdGroups.TryGetValue(enforceGroup, out List<int> groupCardIds))
            {
                foreach (int cardId in groupCardIds)
                {
                    if (cardId != 0 && !cardData.enforceCardIds.Contains(cardId))
                    {
                        cardData.enforceCardIds.Add(cardId);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[CardJSONConverter] enforceGroup not found: {enforceGroup}");
            }
        }

        if (cardObject["enforce"] is JArray enforceArray)
        {
            foreach (JToken token in enforceArray)
            {
                int value = ReadInt(token);
                if (value != 0 && !cardData.enforceCardIds.Contains(value))
                {
                    cardData.enforceCardIds.Add(value);
                }
            }
        }

        cardData.artworkAddress = ReadString(cardObject.SelectToken("addressables.artwork"));
        cardData.effectAddress = ReadString(cardObject.SelectToken("addressables.effect"));
        cardData.soundAddress = ReadString(cardObject.SelectToken("addressables.sound"));

        cardData.effects.Clear();
        if (cardObject["effects"] is JArray effectsArray)
        {
            foreach (JToken token in effectsArray)
            {
                if (!(token is JObject effectObject))
                {
                    continue;
                }

                CardEffectData effect = ReadEffect(effectObject);
                if (effect != null)
                {
                    cardData.effects.Add(effect);
                }
            }
        }
    }

    private CardEffectData ReadEffect(JObject effectObject)
    {
        if (effectObject == null)
        {
            return null;
        }

        // 스키마 호환 포인트:
        // - 구버전: { "type": "Conditional", "condition": { ... } }
        // - 신버전: { "condition": { ... } }   // type 생략
        // 신버전 입력이 들어와도 기존 EffectType 파이프라인을 그대로 타도록
        // type이 비어 있고 condition 블록이 있으면 Conditional로 보정한다.
        string effectTypeRaw = ReadString(effectObject, "type");
        if (string.IsNullOrWhiteSpace(effectTypeRaw) && effectObject["condition"] is JObject)
        {
            // JSON 스키마 변경: Conditional은 type 없이 condition 블록만 올 수 있음
            effectTypeRaw = "Conditional";
        }

        CardEffectData effect = new CardEffectData
        {
            // 문자열 타입 -> 내부 enum 매핑
            type = ParseEffectType(effectTypeRaw),
            target = ParseTargetType(ReadString(effectObject, "target")),
            // onAction 문맥 전달용 메타데이터.
            // 실제 런타임 사용은 별도 로직에서 처리하더라도 우선 데이터는 손실 없이 적재한다.
            subject = ReadString(effectObject, "subject"),
            amount = ReadInt(effectObject, "amount"),
            amountFormula = ReadString(effectObject, "amountFormula"),
            count = ReadInt(effectObject, "count"),
            stat = ReadString(effectObject, "stat"),
            buffId = ReadInt(effectObject, "buffId"),
            duration = ReadInt(effectObject, "duration")
        };

        JToken rawAmount = effectObject["amount"];
        if (string.IsNullOrWhiteSpace(effect.amountFormula) && rawAmount != null && rawAmount.Type == JTokenType.String)
        {
            // amount가 문자열이면 숫자 대신 수식일 가능성이 높으므로 amountFormula로 승격한다.
            // 예: "amount": "discarded * 2"
            effect.amountFormula = rawAmount.Value<string>();
        }

        effect.cardIdList = ReadCardIdList(effectObject);

        if (effectObject["effects"] is JArray subEffectsArray)
        {
            effect.subEffects = new List<CardEffectData>();
            foreach (JToken token in subEffectsArray)
            {
                if (!(token is JObject subEffectObject))
                {
                    continue;
                }

                CardEffectData subEffect = ReadEffect(subEffectObject);
                if (subEffect != null)
                {
                    effect.subEffects.Add(subEffect);
                }
            }
        }

        // 리스트 단일화 정책:
        // effect 단수 키 대신 effects 배열만 사용한다.
        // 다만 런타임 일부 효과(예: ChoiceDiscard)는 nestedEffect를 사용하므로
        // 첫 번째 하위 효과를 nestedEffect로 연결해 실행 호환을 유지한다.
        if (effect.nestedEffect == null && effect.subEffects != null && effect.subEffects.Count > 0)
        {
            effect.nestedEffect = effect.subEffects[0];
        }

        if (effectObject["condition"] is JObject conditionObject)
        {
            effect.conditionData = ReadCondition(conditionObject);
        }

        if (effectObject["onAction"] is JObject onActionObject)
        {
            effect.onAction = ReadEffect(onActionObject);
        }

        return effect;
    }

    private ConditionData ReadCondition(JObject conditionObject)
    {
        // condition 블록을 ConditionData로 변환한다.
        // 지원 형태:
        // - checks + effects / elseEffects (리스트 기반 표준)
        // - successEffect / failEffect / failEffects (레거시 호환 입력 -> 리스트로 흡수)
        if (conditionObject == null)
        {
            return null;
        }

        ConditionData condition = new ConditionData
        {
            mode = ReadString(conditionObject, "mode", "And")
        };

        if (conditionObject["checks"] is JArray checksArray)
        {
            foreach (JToken token in checksArray)
            {
                if (token is JObject checkObject)
                {
                    condition.checks.Add(new CheckData
                    {
                        subject = ReadFirstNonEmptyString(checkObject, "subject", "check", "checks"),
                        property = ReadString(checkObject, "property"),
                        param = ReadString(checkObject, "param"),
                        @operator = ReadString(checkObject, "operator"),
                        value = ReadString(checkObject, "value")
                    });
                }
                else if (token.Type == JTokenType.String)
                {
                    // Fallback support for shorthand check entries.
                    condition.checks.Add(new CheckData
                    {
                        subject = ReadString(token),
                        @operator = "Eq",
                        value = "1"
                    });
                }
            }
        }

        if (conditionObject["effects"] is JArray successEffectsArray)
        {
            condition.successEffects = ReadEffectList(successEffectsArray);
        }

        if (conditionObject["elseEffects"] is JArray elseEffectsArray)
        {
            condition.elseEffects = ReadEffectList(elseEffectsArray);
        }

        // 레거시 단수 키 호환: successEffect/failEffect를 리스트로 병합한다.
        if (conditionObject["successEffect"] is JObject successObject)
        {
            condition.successEffects ??= new List<CardEffectData>();
            CardEffectData parsed = ReadEffect(successObject);
            if (parsed != null)
            {
                condition.successEffects.Insert(0, parsed);
            }
        }

        if (conditionObject["failEffect"] is JObject failObject)
        {
            condition.elseEffects ??= new List<CardEffectData>();
            CardEffectData parsed = ReadEffect(failObject);
            if (parsed != null)
            {
                condition.elseEffects.Insert(0, parsed);
            }
        }

        if (conditionObject["failEffects"] is JArray failEffectsArray)
        {
            List<CardEffectData> parsedFailEffects = ReadEffectList(failEffectsArray);
            if (condition.elseEffects == null || condition.elseEffects.Count == 0)
            {
                condition.elseEffects = parsedFailEffects;
            }
            else
            {
                // failEffect 단수 키를 먼저 넣은 경우 뒤에 이어 붙여 순서를 유지한다.
                condition.elseEffects.AddRange(parsedFailEffects);
            }
        }

        return condition;
    }

    private List<CardEffectData> ReadEffectList(JArray effectsArray)
    {
        // JObject 항목만 안전하게 골라 CardEffectData 리스트로 변환한다.
        // null/비객체 토큰은 조용히 스킵하여 변환 내구성을 높인다.
        List<CardEffectData> result = new List<CardEffectData>();
        if (effectsArray == null)
        {
            return result;
        }

        foreach (JToken token in effectsArray)
        {
            if (!(token is JObject effectObject))
            {
                continue;
            }

            CardEffectData effect = ReadEffect(effectObject);
            if (effect != null)
            {
                result.Add(effect);
            }
        }

        return result;
    }

    private List<int> ReadCardIdList(JObject effectObject)
    {
        // 카드 ID 입력 경로를 통합 처리한다.
        // 우선순위:
        // 1) cardIdGroup -> 그룹 매핑 확장
        // 2) cardId/cardIds -> 단일 또는 배열 값 추가
        // 결과는 중복 없는 List<int>로 반환한다.
        List<int> result = new List<int>();
        if (effectObject == null)
        {
            return result;
        }

        string groupId = ReadString(effectObject, "cardIdGroup");
        if (!string.IsNullOrWhiteSpace(groupId))
        {
            if (cardIdGroups.TryGetValue(groupId, out List<int> groupCardIds))
            {
                foreach (int cardId in groupCardIds)
                {
                    if (cardId != 0 && !result.Contains(cardId))
                    {
                        result.Add(cardId);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[CardJSONConverter] cardIdGroup not found: {groupId}");
            }
        }

        JToken token = effectObject["cardId"] ?? effectObject["cardIds"];
        if (token == null)
        {
            return result;
        }

        if (token is JArray array)
        {
            foreach (JToken item in array)
            {
                int value = ReadInt(item);
                if (value != 0 && !result.Contains(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        int single = ReadInt(token);
        if (single != 0 && !result.Contains(single))
        {
            result.Add(single);
        }

        return result;
    }

    private Dictionary<string, List<int>> LoadCardIdGroups()
    {
        // cardIdGroups.json을 읽어 id -> cardIds 맵을 구성한다.
        // 이 파일은 선택적(optional) 입력이며, 없으면 빈 맵으로 계속 진행한다.
        Dictionary<string, List<int>> groups = new Dictionary<string, List<int>>();
        string filePath = Path.Combine(jsonFolderPath, CardIdGroupsFileName).Replace("\\", "/");

        if (!File.Exists(filePath))
        {
            Debug.Log($"[CardJSONConverter] Optional group file not found: {filePath}");
            return groups;
        }

        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"[CardJSONConverter] Failed to load group file asset: {filePath}");
            return groups;
        }

        JObject root;
        try
        {
            root = JObject.Parse(jsonFile.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardJSONConverter] Invalid group JSON in {filePath}: {ex.Message}");
            return groups;
        }

        if (!(root["groups"] is JArray groupsArray))
        {
            Debug.LogWarning($"[CardJSONConverter] Missing 'groups' array in {filePath}");
            return groups;
        }

        foreach (JToken token in groupsArray)
        {
            if (!(token is JObject groupObject))
            {
                continue;
            }

            string id = ReadString(groupObject, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            List<int> cardIds = new List<int>();
            if (groupObject["cardIds"] is JArray cardIdsArray)
            {
                foreach (JToken cardIdToken in cardIdsArray)
                {
                    int cardId = ReadInt(cardIdToken);
                    if (cardId != 0 && !cardIds.Contains(cardId))
                    {
                        cardIds.Add(cardId);
                    }
                }
            }

            groups[id] = cardIds;
        }

        Debug.Log($"[CardJSONConverter] Loaded cardIdGroups: {groups.Count}");
        return groups;
    }

    private string BuildCardAssetPath(CardData cardData)
    {
        // 카드 데이터에서 파일명을 조합하는 편의 래퍼
        return BuildCardAssetPath(cardData.cardId, cardData.cardName);
    }

    private string BuildCardAssetPath(int cardId, string cardName)
    {
        // 카드 ID+이름으로 저장 경로를 생성한다.
        // 파일명 불가 문자는 SanitizeFileName에서 치환한다.
        string rawName = $"{cardId}_{cardName}.asset";
        string safeName = SanitizeFileName(rawName);
        return Path.Combine(outputPath, safeName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string fileName)
    {
        // 운영체제 파일명 금지 문자를 '_'로 치환해 에셋 생성 실패를 방지한다.
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid.ToString(), "_");
        }

        return fileName;
    }

    private Dictionary<int, CardData> BuildCardIndexById()
    {
        // 출력 폴더의 기존 CardData를 cardId 기준으로 인덱싱한다.
        // 변환 시 동일 cardId를 재사용/갱신하기 위한 핵심 캐시.
        Dictionary<int, CardData> map = new Dictionary<int, CardData>();
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { outputPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (cardData == null || cardData.cardId == 0)
            {
                continue;
            }

            map[cardData.cardId] = cardData;
        }

        return map;
    }

    private static CardCollection GetOrCreateCardCollection()
    {
        // CardCollection.asset의 존재를 보장한다.
        // 없으면 생성해서 반환한다.
        EnsureDirectory("Assets/Resources");

        CardCollection collection = AssetDatabase.LoadAssetAtPath<CardCollection>(CardCollectionPath);
        if (collection != null)
        {
            return collection;
        }

        collection = ScriptableObject.CreateInstance<CardCollection>();
        AssetDatabase.CreateAsset(collection, CardCollectionPath);
        return collection;
    }

    private static void EnsureDirectory(string path)
    {
        // 변환 대상 경로가 없을 때만 생성한다.
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static EffectType ParseEffectType(string type)
    {
        // 방어 로직:
        // type이 비어 있으면 기본값 Damage를 반환한다.
        // 단, ReadEffect에서 condition 유무를 보고 Conditional 보정을 먼저 수행하므로
        // "condition만 있는 효과"가 여기서 잘못 Damage로 떨어지지 않도록 설계되어 있다.
        if (string.IsNullOrWhiteSpace(type))
        {
            return EffectType.Damage;
        }

        switch (type)
        {
            case "Attack": return EffectType.Attack;
            case "Damage": return EffectType.Damage;
            case "Defense":
            case "Barrier": return EffectType.Barrier;
            case "Draw": return EffectType.Draw;
            case "Buff": return EffectType.Buff;
            case "Energy": return EffectType.Energy;
            case "Heal": return EffectType.Heal;
            case "MultiplyDefense":
            case "MultimediaDefense": return EffectType.MultiplyDefense;
            case "ConsumeDefense": return EffectType.ConsumeDefense;
            case "GenerateCard": return EffectType.GenerateCard;
            case "Keyword": return EffectType.Keyword;
            case "DiscardHand": return EffectType.DiscardHand;
            case "ChoiceDiscard": return EffectType.ChoiceDiscard;
            case "Pickup": return EffectType.Pickup;
            case "ExhaustHand": return EffectType.ExhaustHand;
            case "Repeat": return EffectType.Repeat;
            // 대소문자 혼용 데이터 대응
            case "Conditional": return EffectType.Conditional;
            case "conditional": return EffectType.Conditional;
            default: return EffectType.Damage;
        }
    }

    private static TargetType ParseTargetType(string target)
    {
        // JSON 문자열 타겟을 런타임 TargetType으로 정규화한다.
        // 예: "Enemy", "RandEnemy" -> SingleEnemy
        if (string.IsNullOrWhiteSpace(target))
        {
            return TargetType.SingleEnemy;
        }

        switch (target)
        {
            case "AllEnemies": return TargetType.AllEnemies;
            case "SingleEnemy":
            case "Enemy":
            case "RandomEnemy":
            case "RandEnemy": return TargetType.SingleEnemy;
            case "Self": return TargetType.Self;
            case "Hand": return TargetType.Hand;
            case "Discard":
            case "DiscardPile": return TargetType.Discard;
            case "Deck":
            case "DrawPile": return TargetType.Deck;
            case "None":
            case "Selected": return TargetType.None;
            default: return TargetType.SingleEnemy;
        }
    }

    private static string ReadFirstNonEmptyString(JObject obj, params string[] keys)
    {
        // 여러 키 후보 중 첫 유효 문자열을 반환한다.
        // 스키마/대소문자 변형 키를 유연하게 흡수하기 위한 유틸.
        foreach (string key in keys)
        {
            string value = ReadString(obj, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string ReadString(JObject obj, string key, string defaultValue = "")
    {
        // JObject key 접근용 래퍼
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadString(obj[key], defaultValue);
    }

    private static string ReadString(JToken token, string defaultValue = "")
    {
        // JToken을 안전하게 문자열로 변환한다.
        // String 타입이 아니면 ToString() 결과를 사용한다.
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return defaultValue;
        }

        if (token.Type == JTokenType.String)
        {
            return token.Value<string>() ?? defaultValue;
        }

        return token.ToString();
    }

    private static int ReadInt(JObject obj, string key, int defaultValue = 0)
    {
        // JObject key 접근용 정수 래퍼
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadInt(obj[key], defaultValue);
    }

    private static int ReadInt(JToken token, int defaultValue = 0)
    {
        // 정수/실수/문자열 숫자를 모두 허용해 int로 변환한다.
        // 문자열 숫자는 InvariantCulture 기준으로 파싱한다.
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return defaultValue;
        }

        if (token.Type == JTokenType.Integer)
        {
            return token.Value<int>();
        }

        if (token.Type == JTokenType.Float)
        {
            return Mathf.RoundToInt(token.Value<float>());
        }

        if (token.Type == JTokenType.String)
        {
            string raw = token.Value<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return intValue;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return Mathf.RoundToInt(floatValue);
            }
        }

        return defaultValue;
    }

    private static float ReadFloat(JObject obj, string key, float defaultValue = 0f)
    {
        // JObject key 접근용 실수 래퍼
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadFloat(obj[key], defaultValue);
    }

    private static float ReadFloat(JToken token, float defaultValue = 0f)
    {
        // 실수/정수/문자열 숫자를 float으로 변환한다.
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return defaultValue;
        }

        if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
        {
            return token.Value<float>();
        }

        if (token.Type == JTokenType.String)
        {
            string raw = token.Value<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return floatValue;
            }
        }

        return defaultValue;
    }
}

