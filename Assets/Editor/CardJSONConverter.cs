using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// JSON card data to ScriptableObject converter.
/// This class would have taken a fcking week without AI
/// Operation: 1m, Code review: 12h
/// </summary>
public class CardJSONConverter : EditorWindow
{
    private const string DefaultJsonFolderPath = "Assets/Resources/JsonData";
    private const string DefaultOutputPath = "Assets/Data/Cards";
    private const string CardCollectionPath = "Assets/Resources/CardCollection.asset";
    private const string CardGroupsFileName = "cardGroups.json";

    private string jsonFolderPath = DefaultJsonFolderPath;
    private string outputPath = DefaultOutputPath;
    private Dictionary<string, List<int>> cardGroups = new();

    [MenuItem("Tools/TCG/Card JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CardJSONConverter>("Card Converter");
    }

    private static MoveZoneType ParseMoveZoneType(string zone, MoveZoneType defaultZone)
    {
        if (string.IsNullOrWhiteSpace(zone)) {
            return defaultZone;
        }

        switch (zone)
        {
            case "Source": return MoveZoneType.Source;
            case "Hand": return MoveZoneType.Hand;
            case "Deck":
            case "DrawPile": return MoveZoneType.DrawPile;
            case "Discard":
            case "DiscardPile": return MoveZoneType.DiscardPile;
            case "AllCards": return MoveZoneType.AllCards;
            case "CardId": return MoveZoneType.CardId;
            case "Basic": return MoveZoneType.Basic;
            case "Unique": return MoveZoneType.Unique;
            case "AllUnique": return MoveZoneType.AllUnique;
            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown move zone: {zone}. Fallback to {defaultZone}.");
                return defaultZone;
        }
    }

    private static string ResolveMoveZoneStringForParser(string zone, EffectType effectType)
    {
        if (string.IsNullOrWhiteSpace(zone)) {
            return zone;
        }

        if (effectType == EffectType.RandGenerate) {
            return IsMoveZoneString(zone) ? zone : string.Empty;
        }

        if (effectType == EffectType.SelectCard && !IsMoveZoneString(zone)) {
            return "CardId";
        }

        return zone;
    }

    private static bool IsMoveZoneString(string zone)
    {
        if (string.IsNullOrWhiteSpace(zone)) {
            return false;
        }

        switch (zone)
        {
            case "Source":
            case "Hand":
            case "Deck":
            case "DrawPile":
            case "Discard":
            case "DiscardPile":
            case "AllCards":
            case "CardId":
            case "Basic":
            case "Unique":
            case "AllUnique":
                return true;
            default:
                return false;
        }
    }

    private static MovePositionType ParseMovePositionType(string position)
    {
        if (string.IsNullOrWhiteSpace(position)) {
            return MovePositionType.None;
        }

        switch (position)
        {
            case "Top": return MovePositionType.Top;
            case "Random": return MovePositionType.Random;
            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown move position: {position}. Fallback to None.");
                return MovePositionType.None;
        }
    }

    /// <summary>
    /// CI에서 Unity 에디터 없이 커맨드라인으로 실행할 때 사용하는 진입점
    /// Usage: Unity.exe -batchmode -quit -projectPath <path> -executeMethod CardJSONConverter.ConvertAllDefaultJsons
    /// </summary>
    public static void ConvertAllDefaultJsons() {
        CardJSONConverter converter = CreateInstance<CardJSONConverter>();
        converter.jsonFolderPath = DefaultJsonFolderPath;
        converter.outputPath = DefaultOutputPath;
        converter.ConvertJSONToScriptableObjects();
    }


    /// <summary>
    /// Card JSON Converter의 에디터 UI
    /// 경로를 입력받고 버튼 클릭 시 ConvertJSONToScriptableObjects를 호출
    /// </summary>
    private void OnGUI() {
        GUILayout.Label("Card JSON Batch Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        jsonFolderPath = EditorGUILayout.TextField("JSON Folder Path", jsonFolderPath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        GUILayout.Space(10);
        if (GUILayout.Button("Convert All JSONs in Folder", GUILayout.Height(30))) {
            ConvertJSONToScriptableObjects();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Converts all '*Cards.json' files in the selected folder into CardData assets and updates CardCollection.",
            MessageType.Info);
    }

    /// <summary>
    /// JSON 데이터를 ScriptableObject로 변환
    /// </summary>
    private void ConvertJSONToScriptableObjects()
    {
        // 입력 폴더 검증
        if (!Directory.Exists(jsonFolderPath))
        {
            Debug.LogError($"[CardJSONConverter] Folder not found: {jsonFolderPath}");
            EditorUtility.DisplayDialog("Error", "JSON folder path is invalid.", "OK");
            return;
        }

        // 입력 파일 검증
        string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*Cards.json");
        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"No '*Cards.json' found under {jsonFolderPath}", "OK");
            return;
        }

        // 카드 그룹(cardGroups) 로드
        if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
        }
        cardGroups = LoadCardGroups();

        // CardCollection SO 로드
        CardCollection collection = GetOrCreateCardCollection();

        // 기존 CardData 인덱스 로드
        Dictionary<int, CardData> existingById = BuildCardIndexById();

        // 이번 변환 결과를 담을 맵 생성
        Dictionary<int, CardData> updatedById = new();

        // 각 *Cards.json 파일 변환
        int converted = 0;
        foreach (string filePath in jsonFiles) {
            converted += ConvertFileInternal(filePath, existingById, updatedById);
        }

        // CardCollection 정렬 및 갱신
        List<CardData> normalized = new(updatedById.Values);
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

    /// <summary>
    /// *Cards.json 파일을 읽어 CardData 에셋으로 반영
    /// </summary>
    /// <param name="path">변환할 JSON 파일 경로</param>
    /// <param name="existingById">기존 CardData 인덱스</param>
    /// <param name="updatedById">이번 변환 결과를 담을 맵</param>
    /// <returns>변환된 카드 수</returns>
    private int ConvertFileInternal(string path, Dictionary<int, CardData> existingById, Dictionary<int, CardData> updatedById)
    {
        string assetPath = path.Replace("\\", "/");
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (jsonFile == null) {
            Debug.LogWarning($"[CardJSONConverter] Could not load JSON asset: {assetPath}");
            return 0;
        }

        JObject root;
        try {
            root = JObject.Parse(jsonFile.text);
        } catch (Exception ex) {
            Debug.LogError($"[CardJSONConverter] Invalid JSON in {assetPath}: {ex.Message}");
            return 0;
        }

        if ((root["cards"] is not JArray cardsArray)) {
            Debug.LogWarning($"[CardJSONConverter] Missing 'cards' array in {assetPath}");
            return 0;
        }

        int count = 0;
        foreach (JToken token in cardsArray)
        {
            if (token is not JObject cardObject) {
                continue;
            }

            int cardId = ReadRequiredJsonInt(cardObject, "cardId");
            if (cardId <= 0) {
                throw new ArgumentException($"[CardJSONConverter] cardId must be greater than 0 in {assetPath}");
            }

            CardData cardData = GetOrCreateCardData(cardObject, cardId, existingById);
            if (cardData == null) {
                continue;
            }

            UpdateCardData(cardData, cardObject);

            if (!AssetDatabase.Contains(cardData)) {
                string newAssetPath = BuildCardAssetPath(cardData.cardId, cardData.cardName);
                AssetDatabase.CreateAsset(cardData, newAssetPath);
            }
            else {
                EditorUtility.SetDirty(cardData);
            }

            existingById[cardId] = cardData;
            updatedById[cardId] = cardData;
            count++;
        }

        Debug.Log($"[CardJSONConverter] {Path.GetFileName(path)} converted: {count}");
        return count;
    }


    /// <summary>
    /// 카드 SO를 반환하고 없으면 새로 생성
    /// </summary>
    private CardData GetOrCreateCardData(JObject cardObject, int cardId, Dictionary<int, CardData> existingById)
    {
        // 메모리 인덱스에서 먼저 검색
        if (existingById.TryGetValue(cardId, out CardData existing) && existing != null) {
            return existing;
        }

        // 예상 경로에서 에셋 로드 시도
        string candidatePath = BuildCardAssetPath(cardId, ReadJsonString(cardObject, "name"));
        CardData loaded = AssetDatabase.LoadAssetAtPath<CardData>(candidatePath);
        if (loaded != null) {
            return loaded;
        }

        // 없으면 ScriptableObject 인스턴스 생성
        return ScriptableObject.CreateInstance<CardData>();
    }


    /// <summary>
    /// JSON 데이터를 CardData에 매핑
    /// </summary>
    private void UpdateCardData(CardData cardData, JObject cardObject)
    {
        cardData.cardId = ReadRequiredJsonInt(cardObject, "cardId");
        cardData.cardName = ReadRequiredJsonString(cardObject, "name");
        cardData.character = CharacterManager.GetCharacterEnumById(ReadRequiredJsonInt(cardObject, "characterId"));
        cardData.cost = ReadRequiredJsonInt(cardObject, "cost");
        cardData.description = ReadRequiredJsonString(cardObject, "description");

        cardData.enforceCardIds.Clear();
        string enforceGroup = ReadJsonString(cardObject, "enforceGroup");
        if (!string.IsNullOrWhiteSpace(enforceGroup)) {
            if (cardGroups.TryGetValue(enforceGroup, out List<int> groupCardIds)) {
                foreach (int cardId in groupCardIds) {
                    if (cardId != 0 && !cardData.enforceCardIds.Contains(cardId)) {
                        cardData.enforceCardIds.Add(cardId);
                    }
                }
            }
            else {
                Debug.LogWarning($"[CardJSONConverter] enforceGroup not found: {enforceGroup}");
            }
        }

        cardData.keywords.Clear();
        List<int> keywords = ReadJsonIntList(cardObject, "keywords");
        foreach (int keywordId in keywords) {
            if (keywordId > 0 && !cardData.keywords.Contains(keywordId)) {
                cardData.keywords.Add(keywordId);
            }
        }

        cardData.effects.Clear();
        if (cardObject["effects"] is JArray effectsArray) {
            foreach (JToken token in effectsArray) {
                if (token is not JObject effectObject) {
                    continue;
                }

                CardEffectData effect = ReadEffect(effectObject);
                if (effect != null) {
                    cardData.effects.Add(effect);
                }
            }
        }

        cardData.endTurnInHandEffects ??= new List<CardEffectData>();
        cardData.endTurnInHandEffects.Clear();
        if (cardObject["endTurnInHandEffects"] is JArray endTurnInHandEffectsArray) {
            foreach (JToken token in endTurnInHandEffectsArray) {
                if (token is not JObject effectObject) {
                    continue;
                }

                CardEffectData effect = ReadEffect(effectObject);
                if (effect != null) {
                    cardData.endTurnInHandEffects.Add(effect);
                }
            }
        }
    }


    /// <summary>
    /// JSON effect 객체를 CardEffectData로 변환
    /// </summary>
    private CardEffectData ReadEffect(JObject effectObject, bool isOnActionContext = false)
    {
        if (effectObject == null) {
            return null;
        }

        string effectTypeRaw = ReadJsonString(effectObject, "type");
        if (string.IsNullOrWhiteSpace(effectTypeRaw) && effectObject["condition"] is JObject) {
            effectTypeRaw = "Conditional";
        }

        EffectType effectType = ParseEffectType(effectTypeRaw);
        string fromRaw = ReadJsonString(effectObject, "from");
        string toRaw = ReadJsonString(effectObject, "to");
        string targetRaw = ReadJsonString(effectObject, "target");
        string resolvedFromRaw = ResolveMoveZoneStringForParser(fromRaw, effectType);
        ValidateRepeatEffectSchema(effectObject, effectTypeRaw);
        bool hasOnAction = effectObject["onAction"] is JObject || effectObject["onAction"] is JArray;

        // SelectCard는 onAction 밖에서 from을 반드시 가져야 함
        if (effectType == EffectType.SelectCard && !isOnActionContext && string.IsNullOrWhiteSpace(fromRaw)) {
            throw new ArgumentException("[CardJSONConverter] SelectCard effect requires 'from' outside onAction context.");
        }

        // onAction이 있으면 바깥 effect에 subject가 반드시 있어야 함
        if (hasOnAction && !isOnActionContext && string.IsNullOrWhiteSpace(ReadJsonString(effectObject, "subject"))) {
            throw new ArgumentException("[CardJSONConverter] Effect with onAction requires outer 'subject'.");
        }
        

        MoveZoneType fromDefault = effectType == EffectType.SelectCard
            ? MoveZoneType.None
            : MoveZoneType.Source;

        CardEffectData effect = new CardEffectData
        {
            type = effectType,
            target = ParseTargetType(targetRaw),
            from = ParseMoveZoneType(resolvedFromRaw, fromDefault),
            to = ParseMoveZoneType(toRaw, MoveZoneType.None),
            position = ParseMovePositionType(ReadJsonString(effectObject, "position")),
            amount = ReadJsonInt(effectObject, "amount"),
            amountFormula = ReadJsonString(effectObject, "amountFormula"),
            source = ReadJsonString(effectObject, "source"),
            param = ReadJsonString(effectObject, "param"),
            operation = ReadJsonString(effectObject, "operation"),
            upgrade = ReadJsonString(effectObject, "upgrade"),
            durationText = ReadJsonString(effectObject, "duration"),
            effectIndex = ReadJsonInt(effectObject, "effectIndex", -1),
            ampMultiplier = ReadJsonFloat(effectObject, "ampMultiplier", 1f),
            count = ReadJsonInt(effectObject, "count"),
            stat = ReadJsonString(effectObject, "stat"),
            change = ReadJsonString(effectObject, "change"),
            buffId = ReadJsonInt(effectObject, "buffId"),
            buffFilterIds = ReadJsonIntList(effectObject, "buffIds"),
            random = ReadJsonBool(effectObject, "random"),
            duration = ReadJsonInt(effectObject, "duration"),
            cardId = ReadJsonString(effectObject, "cardId"),
            subject = ReadJsonString(effectObject, "subject"),
            characterFilter = ReadJsonString(effectObject, "character", ReadJsonString(effectObject, "characterFilter")),
            timing = ReadJsonString(effectObject, "timing"),
            repeatNextEffect = string.Equals(effectTypeRaw, "Repeat", StringComparison.OrdinalIgnoreCase)
        };

        ValidateCopyEffectSchema(effectObject, effectType, fromRaw, toRaw, targetRaw);
        ValidateKillEffectSchema(effectObject, effectType);
        ValidateLegacyMoveKeywords(effect);

        /*
        if (effect.type == EffectType.Move && string.Equals(effect.subject, "All", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("[CardJSONConverter] Move effect cannot use subject=\"All\". Use amountFormula=\"all\".");
        }

        if (effect.type == EffectType.Move) {
            bool hasAmount = effect.amount > 0;
            bool hasAmountFormula = !string.IsNullOrWhiteSpace(effect.amountFormula);
            bool hasOnActionSubject = isOnActionContext && !string.IsNullOrWhiteSpace(effect.subject);
            if (!hasAmount && !hasAmountFormula && !hasOnActionSubject) {
                throw new ArgumentException("[CardJSONConverter] Move effect requires amount/amountFormula, or subject in onAction context.");
            }
        }
        */

        string cardIdGroup = ReadJsonString(effectObject, "cardIdGroup");
        if (!string.IsNullOrWhiteSpace(cardIdGroup)) {
            if (cardGroups.TryGetValue(cardIdGroup, out List<int> groupCardIds)) {
                effect.formulaCardIdFilter = new List<int>(groupCardIds);
            }
            else {
                Debug.LogWarning($"[CardJSONConverter] cardIdGroup not found: {cardIdGroup}");
            }
        }

        if (effectType == EffectType.RandGenerate && string.IsNullOrWhiteSpace(cardIdGroup) && !string.IsNullOrWhiteSpace(fromRaw)) {
            if (cardGroups.TryGetValue(fromRaw, out List<int> fromCardIds)) {
                effect.formulaCardIdFilter = new List<int>(fromCardIds);
            }
            else if (!IsMoveZoneString(fromRaw))
            {
                Debug.LogWarning($"[CardJSONConverter] from card group not found: {fromRaw}");
            }
        }

        if (effectType == EffectType.SelectCard && string.IsNullOrWhiteSpace(cardIdGroup) && !string.IsNullOrWhiteSpace(fromRaw) && !IsMoveZoneString(fromRaw)) {
            if (cardGroups.TryGetValue(fromRaw, out List<int> fromCardIds)) {
                effect.formulaCardIdFilter = new List<int>(fromCardIds);
            }
            else {
                Debug.LogWarning($"[CardJSONConverter] from card group not found: {fromRaw}");
            }
        }

        if (effectObject["cardIds"] is JArray cardIdsArray) {
            if (effect.formulaCardIdFilter == null) {
                effect.formulaCardIdFilter = new List<int>();
            }

            foreach (JToken cardIdToken in cardIdsArray) {
                int cardId = ReadJsonInt(cardIdToken, 0);
                if (cardId > 0 && !effect.formulaCardIdFilter.Contains(cardId)) {
                    effect.formulaCardIdFilter.Add(cardId);
                }
            }
        }

        if (effectObject["effects"] is JArray subEffectsArray)
        {
            effect.subEffects = new List<CardEffectData>();
            foreach (JToken token in subEffectsArray)
            {
                if (token is not JObject subEffectObject)
                {
                    continue;
                }

                CardEffectData subEffect = ReadEffect(subEffectObject, isOnActionContext);
                if (subEffect != null)
                {
                    effect.subEffects.Add(subEffect);
                }
            }
        }

        if (effectObject["condition"] is JObject conditionObject) {
            effect.conditionData = ReadCondition(conditionObject, isOnActionContext);
        }
        if (effectObject["onAction"] is JArray onActionArray) {
            effect.onAction = ReadEffectList(onActionArray, true);
        }
        else if (effectObject["onAction"] is JObject onActionObject) {
            effect.onAction = new List<CardEffectData>();
            CardEffectData onAction = ReadEffect(onActionObject, true);
            if (onAction != null) {
                effect.onAction.Add(onAction);
            }
        }

        return effect;
    }



    /// <summary>
    /// JSON effect 배열을 List<CardEffectData>로 변환
    /// </summary>
    private List<CardEffectData> ReadEffectList(JArray effectsArray, bool isOnActionContext = false) {
        List<CardEffectData> result = new List<CardEffectData>();
        if (effectsArray == null) {
            return result;
        }

        foreach (JToken token in effectsArray) {
            if (!(token is JObject effectObject)) {
                continue;
            }

            CardEffectData effect = ReadEffect(effectObject, isOnActionContext);
            if (effect != null) {
                result.Add(effect);
            }
        }

        return result;
    }


    /// <summary>
    /// JSON condition 객체를 ConditionData로 변환
    /// </summary>
    private ConditionData ReadCondition(JObject conditionObject, bool isOnActionContext = false)
    {
        if (conditionObject == null) {
            return null;
        }

        ConditionData condition = new();


        if (conditionObject["checks"] is JArray checksArray)
        {
            foreach (JToken token in checksArray)
            {
                if (token is JObject checkObject)
                {
                    condition.checks.Add(new CheckData
                    {
                        subject = ReadJsonString(checkObject, "subject"),
                        property = ReadJsonString(checkObject, "property"),
                        param = ReadJsonString(checkObject, "param"),
                        @operator = ReadJsonString(checkObject, "operator"),
                        value = ReadJsonString(checkObject, "value")
                    });
                }
            }
        }

        if (conditionObject["effects"] is JArray successEffectsArray)
        {
            condition.successEffects = ReadEffectList(successEffectsArray, isOnActionContext);
        }

        if (conditionObject["elseEffects"] is JArray elseEffectsArray)
        {
            condition.elseEffects = ReadEffectList(elseEffectsArray, isOnActionContext);
        }

        return condition;
    }



    /// <summary>
    /// 카드 그룹을 로드
    /// </summary>
    private Dictionary<string, List<int>> LoadCardGroups()
    {
        // cardGroups.json을 읽어 <groupName, cardIds> 맵을 구성
        Dictionary<string, List<int>> groups = new Dictionary<string, List<int>>();
        string filePath = Path.Combine(jsonFolderPath, CardGroupsFileName).Replace("\\", "/");

        // json 파일 존재 여부 확인
        if (!File.Exists(filePath)) {
            Debug.LogError($"[CardJSONConverter] cardGroups file not found: {filePath}");
            return groups;
        }

        // json 파일을 Unity TextAsset으로 로드
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
        if (jsonFile == null) {
            Debug.LogWarning($"[CardJSONConverter] Failed to load group file asset: {filePath}");
            return groups;
        }

        // Text를 다시 JObject로 변환
        JObject root;
        try {
            root = JObject.Parse(jsonFile.text);
        } catch (Exception ex) {
            Debug.LogError($"[CardJSONConverter] Invalid group JSON in {filePath}: {ex.Message}");
            return groups;
        }

        // 최상위 루트의 "groups" 속성이 JArray인지 확인
        if (root["groups"] is not JArray groupsArray) {
            Debug.LogWarning($"[CardJSONConverter] Missing 'groups' array in {filePath}");
            return groups;
        }


        foreach (JToken token in groupsArray)
        {
            // token이 JObject인지 확인
            if ((token is not JObject groupObject)) {
                continue;
            }

            // 그룹 이름 확인
            string groupName = ReadJsonString(groupObject, "groupName");
            // groupName이 비어 있으면 키로 사용할 수 없으므로 스킵
            if (string.IsNullOrWhiteSpace(groupName)) {
                continue;
            }

            // 현재 그룹에 매핑될 카드 ID 목록
            List<int> cardIds = new();

            // cardIds가 배열일 때만 순회
            if (groupObject["cardIds"] is JArray cardIdsArray) {
                foreach (JToken cardIdToken in cardIdsArray) {
                    int cardId = ReadJsonInt(cardIdToken);
                    if (cardId != 0 && !cardIds.Contains(cardId)) {
                        cardIds.Add(cardId);
                    }
                }
            }

            // 최종적으로 groups 딕셔너리에 반영
            // 동일 groupName이 이미 있으면 최신 값으로 덮어씀
            groups[groupName] = cardIds;
        }

        Debug.Log($"[CardJSONConverter] Loaded cardGroups: {groups.Count}");
        return groups;
    }



    // 카드 ID와 이름으로 asset 경로를 생성
    // 파일명 금지 문자는 SanitizeFileName에서 치환
    private string BuildCardAssetPath(int cardId, string cardName)
    {
        string rawName = $"{cardId}_{cardName}.asset";
        string safeName = SanitizeFileName(rawName);
        return Path.Combine(outputPath, safeName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string fileName)
    {
        // 운영체제 파일명 금지 문자를 '_'로 치환해 asset 생성 실패를 방지
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid.ToString(), "_");
        }

        return fileName;
    }


    /// <summary>
    /// CardData SO들을 모아 Dictionary로 반환
    /// </summary>
    /// <returns> CardData Dictionary </returns>
    private Dictionary<int, CardData> BuildCardIndexById()
    {
        // Key:cardId, Value:CardData Asset
        Dictionary<int, CardData> map = new();

        // outputPath 아래 CardData 타입 에셋 GUID 목록 조회
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { outputPath });

        // GUID를 경로로 바꿔 로드한 뒤 map에 추가
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (cardData == null || cardData.cardId == 0) {
                continue;
            }
            map[cardData.cardId] = cardData;
        }
        return map;
    }


    /// <summary>
    /// CardCollection.asset을 보장하고 반환
    /// </summary>
    private CardCollection GetOrCreateCardCollection()
    {
        if (!Directory.Exists("Assets/Resources")) {
            Directory.CreateDirectory("Assets/Resources");
        }

        CardCollection collection = AssetDatabase.LoadAssetAtPath<CardCollection>(CardCollectionPath);

        if (collection != null) {
            return collection;
        }

        collection = ScriptableObject.CreateInstance<CardCollection>();
        AssetDatabase.CreateAsset(collection, CardCollectionPath);
        return collection;
    }


    /// <summary>
    /// 문자열을 EffectType으로 변환
    /// </summary>
    /// <param name="type">이펙트 이름</param>
    /// <returns>이펙트 이름에 대응하는 EffectType</returns>
    /// <exception cref="ArgumentException">필수 값이 잘못된 경우 발생</exception>
    private static EffectType ParseEffectType(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) {
            throw new ArgumentException("[CardJSONConverter] Missing required effect type.");
        }

        switch (type)
        {
            case "Attack": return EffectType.Attack;
            case "Damage": return EffectType.Damage;
            case "Barrier": return EffectType.Barrier;
            case "Draw": return EffectType.Draw;
            case "DrawBasic": return EffectType.DrawBasic;
            case "DrawCharacter": return EffectType.DrawCharacter;
            case "Buff": return EffectType.Buff;
            case "Heal": return EffectType.Heal;
            case "GenerateCard": return EffectType.GenerateCard;
            case "ExhaustCard": return EffectType.ExhaustCard;
            case "Conditional": return EffectType.Conditional;
            case "Repeat": return EffectType.Repeat;
            case "ReduceCost": return EffectType.ReduceCost;

            case "Move": return EffectType.Move;
            case "Copy": return EffectType.Copy;
            case "RandGenerate": return EffectType.RandGenerate;
            case "Keep": return EffectType.Keep;
            case "Cost": return EffectType.Cost;
            case "CreateCard": return EffectType.Repeat; // 추후 삭제 또는 수정 예정
            case "SelectCard": return EffectType.SelectCard;
            case "Upgrade": return EffectType.Upgrade;
            case "MixBuff": return EffectType.MixBuff;
            case "ModifyCards": return EffectType.ModifyCards;
            case "ModifyCard": return EffectType.ModifyCard;
            case "Kill": return EffectType.Kill;
            case "TakeDamage":
                throw new ArgumentException("[CardJSONConverter] TakeDamage effect is no longer supported. Use Damage.");
            case "ChangeStat": return EffectType.ChangeStat;
            case "ExtraTurn": return EffectType.ExtraTurn;
            case "Stamina": return EffectType.Stamina;
            case "MultiplyBarrier": return EffectType.MultiplyBarrier;
            case "Trigger": return EffectType.Trigger;
            case "RemoveBuff": return EffectType.RemoveBuff;
            case "Scry": return EffectType.Scry;
            case "OnAttackGainStrength": return EffectType.OnAttackGainStrength;
            case "TransformCards":
            case "TransformMonsterCards": return EffectType.TransformCards;
            case "EnemyHpLossHealPlayer": return EffectType.EnemyHpLossHealPlayer;
            case "Party": return EffectType.Party;

            default:
                throw new ArgumentException($"[CardJSONConverter] Unsupported effect type: {type}");
        }
    }



    /// <summary>
    /// 문자열을 TargetType으로 변환
    /// </summary>
    /// <param name="target">타겟 이름</param>
    /// <returns>타겟 이름에 대응하는 TargetType</returns>
    /// <exception cref="ArgumentException">필수 값이 잘못된 경우 발생</exception>
    private static TargetType ParseTargetType(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) {
            return TargetType.None;
        }

        switch (target)
        {
            case "AllEnemies": return TargetType.AllEnemies;
            case "SingleEnemy": return TargetType.SingleEnemy;
            case "RandomEnemy":
            case "RandEnemy": return TargetType.RandomEnemy;
            case "Self": return TargetType.Self;
            case "ThisCard": return TargetType.None;
            case "Hand": return TargetType.Hand;
            case "Discard":
            case "DiscardPile": return TargetType.Discard;
            case "Deck":
            case "DrawPile": return TargetType.Deck;
            case "None":
            case "Selected": return TargetType.None;
            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown target type: {target}. Fallback to None.");
                return TargetType.None;
        }
    }

    private static void ValidateLegacyMoveKeywords(CardEffectData effect)
    {
        if (effect == null) {
            return;
        }

        if (!string.IsNullOrWhiteSpace(effect.amountFormula)
            && effect.amountFormula.IndexOf("discarded", StringComparison.OrdinalIgnoreCase) >= 0) {
            throw new ArgumentException("[CardJSONConverter] 'discarded' formula keyword is no longer supported. Use 'moved'.");
        }

        if (string.Equals(effect.subject, "DiscardedCount", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("[CardJSONConverter] 'DiscardedCount' subject is no longer supported. Use 'MovedCount'.");
        }
    }

    private static void ValidateRepeatEffectSchema(JObject effectObject, string effectTypeRaw)
    {
        if (!string.Equals(effectTypeRaw, "Repeat", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        bool hasAmount = HasJsonValue(effectObject, "amount");
        bool hasAmountFormula = !string.IsNullOrWhiteSpace(ReadJsonString(effectObject, "amountFormula"));
        if (!hasAmount && !hasAmountFormula) {
            throw new ArgumentException("[CardJSONConverter] Repeat effect requires 'amount' or 'amountFormula'.");
        }

        if (HasJsonValue(effectObject, "count")) {
            throw new ArgumentException("[CardJSONConverter] Repeat effect no longer supports 'count'. Use 'amount' or 'amountFormula'.");
        }

        if (effectObject["effects"] is JArray) {
            throw new ArgumentException("[CardJSONConverter] Repeat effect no longer supports nested 'effects'. Flatten it so Repeat is followed by the next effect.");
        }

        if (effectObject["onAction"] is JObject || effectObject["onAction"] is JArray) {
            throw new ArgumentException("[CardJSONConverter] Repeat effect no longer supports 'onAction'. Flatten it so Repeat is followed by the next effect.");
        }

        if (HasJsonValue(effectObject, "subject")) {
            throw new ArgumentException("[CardJSONConverter] Repeat effect no longer supports 'subject'. Flatten it so Repeat is followed by the next effect.");
        }
    }

    private static void ValidateCopyEffectSchema(JObject effectObject, EffectType effectType, string fromRaw, string toRaw, string targetRaw)
    {
        if (effectType != EffectType.Copy) {
            return;
        }

        bool hasLegacyCardId = HasJsonValue(effectObject, "cardId");
        bool hasLegacyTarget = !string.IsNullOrWhiteSpace(targetRaw);

        if (hasLegacyCardId || hasLegacyTarget) {
            List<string> legacyFields = new List<string>();
            if (hasLegacyCardId) legacyFields.Add("cardId");
            if (hasLegacyTarget) legacyFields.Add("target");
            throw new ArgumentException($"[CardJSONConverter] Copy effect must use from/to. Remove legacy field(s): {string.Join(", ", legacyFields)}.");
        }

        if (string.IsNullOrWhiteSpace(fromRaw) || string.IsNullOrWhiteSpace(toRaw)) {
            throw new ArgumentException("[CardJSONConverter] Copy effect requires both 'from' and 'to'.");
        }

        if (string.Equals(fromRaw, "CardId", StringComparison.OrdinalIgnoreCase)) {
            if (effectObject["cardIds"] is not JArray cardIds || cardIds.Count == 0) {
                throw new ArgumentException("[CardJSONConverter] Copy effect with from=\"CardId\" requires non-empty 'cardIds'.");
            }
        }
    }

    private static void ValidateKillEffectSchema(JObject effectObject, EffectType effectType)
    {
        if (effectType != EffectType.Kill) {
            return;
        }

        bool hasAmount = HasJsonValue(effectObject, "amount");
        bool hasAmountFormula = !string.IsNullOrWhiteSpace(ReadJsonString(effectObject, "amountFormula"));
        if (hasAmount || hasAmountFormula) {
            throw new ArgumentException("[CardJSONConverter] Kill effect no longer supports amount or amountFormula. Use Damage before Kill if you need self damage.");
        }
    }

    private static bool HasJsonValue(JObject obj, string key)
    {
        if (obj == null || string.IsNullOrWhiteSpace(key)) {
            return false;
        }

        JToken token = obj[key];
        if (token == null) {
            return false;
        }

        return token.Type != JTokenType.Null && token.Type != JTokenType.Undefined;
    }


    /// <summary>
    /// JObject에서 문자열 값을 읽는 헬퍼
    /// </summary>
    private static string ReadJsonString(JObject obj, string key, string defaultValue = "")
    {
        if (obj == null) {
            return defaultValue;
        }
        return ReadJsonString(obj[key], defaultValue);
    }

    /// <summary>
    /// JToken을 안전하게 문자열로 변환
    /// String 타입이 아니면 ToString 결과를 사용
    /// </summary>
    private static string ReadJsonString(JToken token, string defaultValue = "")
    {
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

    /// <summary>
    /// JObject에서 정수 값을 읽는 헬퍼
    /// </summary>
    private static int ReadJsonInt(JObject obj, string key, int defaultValue = 0)
    {
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadJsonInt(obj[key], defaultValue);
    }

    /// <summary>
    /// JToken을 안전하게 정수로 변환
    /// 정수/실수/문자열 숫자를 모두 허용
    /// 문자열 숫자는 InvariantCulture 기준으로 파싱
    /// </summary>
    private static int ReadJsonInt(JToken token, int defaultValue = 0)
    {
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

    private static bool ReadJsonBool(JObject obj, string key, bool defaultValue = false)
    {
        if (obj == null)
        {
            return defaultValue;
        }

        JToken token = obj[key];
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return defaultValue;
        }

        if (token.Type == JTokenType.Boolean)
        {
            return token.Value<bool>();
        }

        if (token.Type == JTokenType.String)
        {
            string raw = token.Value<string>();
            if (bool.TryParse(raw, out bool boolValue))
            {
                return boolValue;
            }
        }

        return defaultValue;
    }

    private static List<int> ReadJsonIntList(JObject obj, string key)
    {
        if (obj == null)
        {
            return new List<int>();
        }

        return ReadJsonIntList(obj[key]);
    }

    private static List<int> ReadJsonIntList(JToken token)
    {
        List<int> values = new List<int>();
        if (token is not JArray array)
        {
            return values;
        }

        foreach (JToken item in array)
        {
            int parsed = ReadJsonInt(item, int.MinValue);
            if (parsed != int.MinValue)
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    private static string ReadRequiredJsonString(JObject obj, string key)
    {
        if (obj == null || obj[key] == null || obj[key].Type == JTokenType.Null || obj[key].Type == JTokenType.Undefined) {
            throw new ArgumentException($"[CardJSONConverter] Missing required string field: {key}");
        }

        return ReadJsonString(obj[key], string.Empty);
    }

    private static int ReadRequiredJsonInt(JObject obj, string key)
    {
        if (obj == null || obj[key] == null || obj[key].Type == JTokenType.Null || obj[key].Type == JTokenType.Undefined) {
            throw new ArgumentException($"[CardJSONConverter] Missing required int field: {key}");
        }

        JToken token = obj[key];
        int parsed = ReadJsonInt(token, int.MinValue);
        if (parsed == int.MinValue) {
            throw new ArgumentException($"[CardJSONConverter] Required int field is invalid: {key}");
        }

        return parsed;
    }

    /// <summary>
    /// JObject에서 실수 값을 읽는 헬퍼
    /// </summary>
    private static float ReadJsonFloat(JObject obj, string key, float defaultValue = 0f)
    {
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadJsonFloat(obj[key], defaultValue);
    }

    /// <summary>
    /// JToken을 안전하게 실수로 변환
    /// 실수/정수/문자열 숫자를 float로 변환
    /// </summary>
    private static float ReadJsonFloat(JToken token, float defaultValue = 0f)
    {
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


