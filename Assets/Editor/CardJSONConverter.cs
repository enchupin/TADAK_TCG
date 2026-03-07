using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Mono.Cecil.Cil;
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
    /// CI?먯꽌 Unity?놁씠 而ㅻ㎤?쒕씪?몄뿉???먮룞?쇰줈 ?ㅽ뻾?????덈뒗 吏꾩엯??
    /// Usage: Unity.exe -batchmode -quit -projectPath <path> -executeMethod CardJSONConverter.ConvertAllDefaultJsons
    /// </summary>
    public static void ConvertAllDefaultJsons() {
        CardJSONConverter converter = CreateInstance<CardJSONConverter>();
        converter.jsonFolderPath = DefaultJsonFolderPath;
        converter.outputPath = DefaultOutputPath;
        converter.ConvertJSONToScriptableObjects();
    }


    /// <summary>
    /// Draw Card JSON Converter Tool's Editor UI
    /// 寃쎈줈 ?낅젰媛믪쓣 諛쏄퀬 踰꾪듉 ?대┃ ??蹂??濡쒖쭅(ConvertJSONToScriptableObjects)???몄텧
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
    /// JSON ?곗씠?곕? SO濡?蹂??
    /// </summary>
    private void ConvertJSONToScriptableObjects()
    {
        // ?낅젰 ?대뜑 寃利?
        if (!Directory.Exists(jsonFolderPath))
        {
            Debug.LogError($"[CardJSONConverter] Folder not found: {jsonFolderPath}");
            EditorUtility.DisplayDialog("Error", "JSON folder path is invalid.", "OK");
            return;
        }

        // ?낅젰 ?뚯씪 寃利?
        string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*Cards.json");
        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"No '*Cards.json' found under {jsonFolderPath}", "OK");
            return;
        }

        // 移대뱶 洹몃９(cardGroups) 濡쒕뱶
        if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
        }
        cardGroups = LoadCardGroups();

        // CardData瑜??몃뜳?깊븯??肄쒕젆??SO?????
        CardCollection collection = GetOrCreateCardCollection();

        // 湲곗〈??議댁옱?섎뜕 CardCollection SO ?뚯씪 濡쒕뱶
        Dictionary<int, CardData> existingById = BuildCardIndexById();

        // ?덈줈??CardCollection SO ?뚯씪 濡쒕뱶瑜??꾪븳 媛앹껜 ?앹꽦
        Dictionary<int, CardData> updatedById = new();

        // 媛?*Cards.json ?뚯씪 蹂??
        int converted = 0;
        foreach (string filePath in jsonFiles) {
            converted += ConvertFileInternal(filePath, existingById, updatedById);
        }

        // CardCollection ?ш뎄???뺣젹/???
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
    /// *Cards.json ?뚯씪(紐⑤뱺 罹먮┃?곗쓽 移대뱶 ?곗씠?곕? ?닿퀬 ?덈뒗 json ?뚯씪)???쎌뼱 CardData ?먯뀑?쇰줈 諛섏쁺
    /// </summary>
    /// <param name="path">  </param>
    /// <param name="existingById"> 湲곗〈 ?먯뀑 ?ъ궗?⑹쓣 ?꾪븳 ?몃뜳??</param>
    /// <param name="updatedById"> ?대쾲 蹂???ъ씠?댁뿉???ㅼ젣 媛깆떊??移대뱶 吏묓빀 </param>
    /// <returns></returns>
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
    /// 移대뱶 SO 諛섑솚 (湲곗〈 ?뚯씪???녿떎硫??앹꽦?섏뿬 諛섑솚)
    /// </summary>
    private CardData GetOrCreateCardData(JObject cardObject, int cardId, Dictionary<int, CardData> existingById)
    {
        // 硫붾え由??몃뜳??existingById)?먯꽌 ?곗꽑 寃??
        if (existingById.TryGetValue(cardId, out CardData existing) && existing != null) {
            return existing;
        }

        // ?덉긽 寃쎈줈(candidatePath)?먯꽌 ?먯뀑 濡쒕뱶 ?쒕룄
        string candidatePath = BuildCardAssetPath(cardId, ReadJsonString(cardObject, "name"));
        CardData loaded = AssetDatabase.LoadAssetAtPath<CardData>(candidatePath);
        if (loaded != null) {
            return loaded;
        }

        // ?놁쑝硫???ScriptableObject ?몄뒪?댁뒪 諛섑솚
        return ScriptableObject.CreateInstance<CardData>();
    }


    /// <summary>
    /// Json ?곗씠?곕? CardData ?대옒?ㅼ뿉 留ㅽ븨
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
    }


    /// <summary>
    /// Json ?뚯씪??effects ?ㅼ썙?쒕? CardEffectData ??낆쑝濡?諛섑솚
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
        bool hasOnAction = effectObject["onAction"] is JObject || effectObject["onAction"] is JArray;

        // SelectCard Effect??from ?ㅼ썙?쒕? ?뚯쑀?섎룄濡?媛뺤젣
        if (effectType == EffectType.SelectCard && !isOnActionContext && string.IsNullOrWhiteSpace(fromRaw)) {
            throw new ArgumentException("[CardJSONConverter] SelectCard effect requires 'from' outside onAction context.");
        }

        // onACtion? Subject ?ㅼ썙?쒕? ?뚯쑀?섎룄濡?媛뺤젣
        if (hasOnAction && string.IsNullOrWhiteSpace(ReadJsonString(effectObject, "subject"))) {
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
            count = ReadJsonInt(effectObject, "count"),
            stat = ReadJsonString(effectObject, "stat"),
            buffId = ReadJsonInt(effectObject, "buffId"),
            duration = ReadJsonInt(effectObject, "duration"),
            cardId = ReadJsonString(effectObject, "cardId"),
            subject = ReadJsonString(effectObject, "subject")
        };

        ValidateCopyEffectSchema(effectObject, effectType, fromRaw, toRaw, targetRaw);

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
    /// Json?뚯씪??effects ?ㅼ썙?쒕? List<CardEffectData> ??낆쑝濡?諛섑솚
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
    /// Json?뚯씪??condtion ?ㅼ썙?쒕? CoditinoData Class濡?蹂??
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
    /// 移대뱶 洹몃９ 濡쒕뱶
    /// </summary>
    private Dictionary<string, List<int>> LoadCardGroups()
    {
        // cardGroups.json???쎌뼱 <groupName, cardIds> 留듭쓣 援ъ꽦
        Dictionary<string, List<int>> groups = new Dictionary<string, List<int>>();
        string filePath = Path.Combine(jsonFolderPath, CardGroupsFileName).Replace("\\", "/");

        // json?뚯씪 議댁옱 ?뺤씤
        if (!File.Exists(filePath)) {
            Debug.LogError($"[CardJSONConverter] cardGroups file not found: {filePath}");
            return groups;
        }

        // jsonFile??Unity Editor Text ??낆쑝濡?濡쒕뱶
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
        if (jsonFile == null) {
            Debug.LogWarning($"[CardJSONConverter] Failed to load group file asset: {filePath}");
            return groups;
        }

        // Text瑜??ㅼ떆 JObject濡?蹂??
        JObject root;
        try {
            root = JObject.Parse(jsonFile.text);
        } catch (Exception ex) {
            Debug.LogError($"[CardJSONConverter] Invalid group JSON in {filePath}: {ex.Message}");
            return groups;
        }

        // 理쒖긽??猷⑦듃??"groups" ?띿꽦??JArray ??낆씤吏 ?뺤씤
        if (root["groups"] is not JArray groupsArray) {
            Debug.LogWarning($"[CardJSONConverter] Missing 'groups' array in {filePath}");
            return groups;
        }


        foreach (JToken token in groupsArray)
        {
            // token??JObject ??낆씤吏 ?뺤씤
            if ((token is not JObject groupObject)) {
                continue;
            }

            // 洹몃９ ?앸퀎??groupName)瑜??뺤씤
            string groupName = ReadJsonString(groupObject, "groupName");
            // groupName??鍮꾩뼱 ?덉쑝硫??뺤뀛?덈━ ?ㅻ줈 ?ъ슜?????놁쑝誘濡??쒖쇅
            if (string.IsNullOrWhiteSpace(groupName)) {
                continue;
            }

            // ?꾩옱 洹몃９??留ㅽ븨??移대뱶 ID 紐⑸줉???댁쓣 由ъ뒪??
            List<int> cardIds = new();

            // cardIds ?ㅺ? 諛곗뿴?몄? ?뺤씤???ㅼ뿉留??쒗쉶
            if (groupObject["cardIds"] is JArray cardIdsArray) {
                foreach (JToken cardIdToken in cardIdsArray) { // cardIds 諛곗뿴??媛???ぉ(移대뱶 ID ?꾨낫)???쒗쉶
                    int cardId = ReadJsonInt(cardIdToken); // ?レ옄/臾몄옄???뺥깭 ?낅젰???덉쟾?섍쾶 int濡?蹂??
                    if (cardId != 0 && !cardIds.Contains(cardId)) { // 0? 臾댄슚 媛믪쑝濡?痍④툒?섍퀬, 以묐났 ID????踰덈쭔 異붽?
                        cardIds.Add(cardId); // 寃利앹쓣 ?듦낵??移대뱶 ID瑜?由ъ뒪?몄뿉 異붽?
                    }
                }
            }

            // 理쒖쥌?곸쑝濡?groups ?뺤뀛?덈━??<groupName, cardIds> 留ㅽ븨?????
            // ?숈씪 groupName???대? ?덉쑝硫?理쒖떊 媛믪쑝濡??낅뜲?댄듃
            groups[groupName] = cardIds;
        }

        Debug.Log($"[CardJSONConverter] Loaded cardGroups: {groups.Count}");
        return groups;
    }



    // 移대뱶 ID+?대쫫?쇰줈 ???寃쎈줈瑜??앹꽦
    // ?뚯씪紐?遺덇? 臾몄옄??SanitizeFileName?먯꽌 移섑솚
    private string BuildCardAssetPath(int cardId, string cardName)
    {
        string rawName = $"{cardId}_{cardName}.asset";
        string safeName = SanitizeFileName(rawName);
        return Path.Combine(outputPath, safeName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string fileName)
    {
        // ?댁쁺泥댁젣 ?뚯씪紐?湲덉? 臾몄옄瑜?'_'濡?移섑솚???먯뀑 ?앹꽦 ?ㅽ뙣瑜?諛⑹?
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid.ToString(), "_");
        }

        return fileName;
    }


    /// <summary>
    /// CardData SO?뚯씪??紐⑥븘??Dictionary瑜?諛섑솚
    /// </summary>
    /// <returns> CardData Dictionary </returns>
    private Dictionary<int, CardData> BuildCardIndexById()
    {
        // Key:cardId, Value:CardData Asset
        Dictionary<int, CardData> map = new();

        // map???앹꽦?섏뿬 outputPath ?꾨옒?먯꽌 CardData ??낆쓽 GUID(SO?뚯씪??二쇱냼) 紐⑸줉 議고쉶
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { outputPath });

        // 二쇱냼瑜?string?쇰줈 蹂?섑븯??濡쒕뱶 ??map??異붽?
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
    /// CardCollection.asset瑜?蹂댁옣(?놁쑝硫??앹꽦)?섏뿬 諛섑솚
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
    /// Parsing EffectType
    /// </summary>
    /// <param name="type"> ?댄럺???대쫫 </param>
    /// <returns> ?댄럺???대쫫???대떦?섎뒗 EffectType </returns>
    /// <exception cref="ArgumentException"> type??null?닿굅??鍮꾩뼱?덉쓣 寃쎌슦 ?덉쇅瑜?諛쒖깮 </exception>
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
            case "Draw": return EffectType.Draw; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "DrawBasic": return EffectType.DrawBasic;
            case "DrawCharacter": return EffectType.DrawCharacter;
            case "Buff": return EffectType.Buff; 
            case "Heal": return EffectType.Heal; 
            case "GenerateCard": return EffectType.GenerateCard; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Keyword": return EffectType.Keyword; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "DiscardHand":
                throw new ArgumentException("[CardJSONConverter] DiscardHand effect is no longer supported. Use Move with from=\"Hand\" and to=\"DiscardPile\".");
            case "Pickup": return EffectType.Pickup; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ExhaustHand": return EffectType.ExhaustHand; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Conditional": return EffectType.Conditional; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Repeat": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ReduceCost": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙

            case "Move": return EffectType.Move;
            case "Copy": return EffectType.Copy;
            case "RandGenerate": return EffectType.RandGenerate;
            case "ChoiceHand":
                throw new ArgumentException("[CardJSONConverter] ChoiceHand effect is no longer supported. Use SelectCard with from=\"Hand\".");
            case "Discard": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Keep": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Cost": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "CreateCard": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ChoiceCard":
            case "SelectCard": return EffectType.SelectCard;
            case "Upgrade": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "MixBuff": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ModifyCards": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ModifyCard": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Kill": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ChangeStat": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "ExtraTurn": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Stamina": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "SelectEnemy": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "TransferStats": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "MultiplyBarrier": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Trigger": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "RemoveBuff": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙
            case "Scry": return EffectType.Scry;
            case "DrawnCard": return EffectType.Repeat; // 異뷀썑 ??젣 or ?섏젙 ?덉젙

            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown effect type: {type}. Fallback to Repeat.");
                return EffectType.Repeat;
        }
    }



    /// <summary>
    /// Parsing TargetType
    /// </summary>
    /// <param name="target"> ?寃??대쫫 </param>
    /// <returns> ?寃??대쫫???대떦?섎뒗 TargetType 諛섑솚 </returns>
    /// <exception cref="ArgumentException"> target??null?닿굅??鍮꾩뼱?덉쑝硫??덉쇅 諛쒖깮 </exception>
    private static TargetType ParseTargetType(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) {
            return TargetType.None;
        }

        switch (target)
        {
            case "AllEnemies": return TargetType.AllEnemies;
            case "SingleEnemy":
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
            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown target type: {target}. Fallback to None.");
                return TargetType.None;
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
    /// JObject key ?묎렐???섑띁
    /// </summary>
    private static string ReadJsonString(JObject obj, string key, string defaultValue = "")
    {
        if (obj == null) {
            return defaultValue;
        }
        return ReadJsonString(obj[key], defaultValue);
    }

    /// <summary>
    /// JToken???덉쟾?섍쾶 臾몄옄?대줈 蹂??
    /// String ??낆씠 ?꾨땲硫?ToString() 寃곌낵瑜??ъ슜
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
    /// JObject key ?묎렐???섑띁
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
    /// JToken???덉쟾?섍쾶 ?뺤닔濡?蹂??
    /// ?뺤닔/?ㅼ닔/臾몄옄???レ옄瑜?紐⑤몢 ?덉슜??int濡?蹂??
    /// 臾몄옄???レ옄??InvariantCulture 湲곗??쇰줈 ?뚯떛
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
    /// JObject key ?묎렐???섑띁
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
    /// JToken???덉쟾?섍쾶 ?ㅼ닔濡?蹂??
    /// ?ㅼ닔/?뺤닔/臾몄옄???レ옄瑜?float?쇰줈 蹂??
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


