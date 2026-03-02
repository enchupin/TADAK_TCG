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
            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown move zone: {zone}. Fallback to {defaultZone}.");
                return defaultZone;
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
    /// CI에서 Unity없이 커맨드라인에서 자동으로 실행할 수 있는 진입점
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
    /// 경로 입력값을 받고 버튼 클릭 시 변환 로직(ConvertJSONToScriptableObjects)을 호출
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
    /// JSON 데이터를 SO로 변환
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

        // CardData를 인덱싱하여 콜렉션 SO에 저장
        CardCollection collection = GetOrCreateCardCollection();

        // 기존에 존재하던 CardCollection SO 파일 로드
        Dictionary<int, CardData> existingById = BuildCardIndexById();

        // 새로운 CardCollection SO 파일 로드를 위한 객체 생성
        Dictionary<int, CardData> updatedById = new();

        // 각 *Cards.json 파일 변환
        int converted = 0;
        foreach (string filePath in jsonFiles) {
            converted += ConvertFileInternal(filePath, existingById, updatedById);
        }

        // CardCollection 재구성/정렬/저장
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
    /// *Cards.json 파일(모든 캐릭터의 카드 데이터를 담고 있는 json 파일)을 읽어 CardData 에셋으로 반영
    /// </summary>
    /// <param name="path">  </param>
    /// <param name="existingById"> 기존 에셋 재사용을 위한 인덱스 </param>
    /// <param name="updatedById"> 이번 변환 사이클에서 실제 갱신된 카드 집합 </param>
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
    /// 카드 SO 반환 (기존 파일이 없다면 생성하여 반환)
    /// </summary>
    private CardData GetOrCreateCardData(JObject cardObject, int cardId, Dictionary<int, CardData> existingById)
    {
        // 메모리 인덱스(existingById)에서 우선 검색
        if (existingById.TryGetValue(cardId, out CardData existing) && existing != null) {
            return existing;
        }

        // 예상 경로(candidatePath)에서 에셋 로드 시도
        string candidatePath = BuildCardAssetPath(cardId, ReadJsonString(cardObject, "name"));
        CardData loaded = AssetDatabase.LoadAssetAtPath<CardData>(candidatePath);
        if (loaded != null) {
            return loaded;
        }

        // 없으면 새 ScriptableObject 인스턴스 반환
        return ScriptableObject.CreateInstance<CardData>();
    }


    /// <summary>
    /// Json 데이터를 CardData 클래스에 매핑
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
    /// Json 파일의 effects 키워드를 CardEffectData 타입으로 반환
    /// </summary>
    private CardEffectData ReadEffect(JObject effectObject, bool isOnActionContext = false)
    {
        if (effectObject == null) {
            return null;
        }

        string effectTypeRaw = ReadJsonString(effectObject, "type");
        if (effectObject["condition"] is JObject) {
            effectTypeRaw = "Conditional";
        }

        CardEffectData effect = new CardEffectData
        {
            type = ParseEffectType(effectTypeRaw),
            target = ParseTargetType(ReadJsonString(effectObject, "target")),
            subject = ReadJsonString(effectObject, "subject"),
            from = ParseMoveZoneType(ReadJsonString(effectObject, "from"), MoveZoneType.Source),
            to = ParseMoveZoneType(ReadJsonString(effectObject, "to"), MoveZoneType.None),
            position = ParseMovePositionType(ReadJsonString(effectObject, "position")),
            amount = ReadJsonInt(effectObject, "amount"),
            amountFormula = ReadJsonString(effectObject, "amountFormula"),
            count = ReadJsonInt(effectObject, "count"),
            stat = ReadJsonString(effectObject, "stat"),
            buffId = ReadJsonInt(effectObject, "buffId"),
            duration = ReadJsonInt(effectObject, "duration")
        };

        if (!string.IsNullOrWhiteSpace(effect.subject) && !isOnActionContext) {
            throw new ArgumentException("[CardJSONConverter] 'subject' is only allowed inside onAction effects.");
        }

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

        string cardIdGroup = ReadJsonString(effectObject, "cardIdGroup");
        if (!string.IsNullOrWhiteSpace(cardIdGroup)) {
            if (cardGroups.TryGetValue(cardIdGroup, out List<int> groupCardIds)) {
                effect.formulaCardIdFilter = new List<int>(groupCardIds);
            }
            else {
                Debug.LogWarning($"[CardJSONConverter] cardIdGroup not found: {cardIdGroup}");
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
    /// Json파일의 effects 키워드를 List<CardEffectData> 타입으로 반환
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
    /// Json파일의 condtion 키워드를 CoditinoData Class로 변환
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
    /// 카드 그룹 로드
    /// </summary>
    private Dictionary<string, List<int>> LoadCardGroups()
    {
        // cardGroups.json을 읽어 <groupName, cardIds> 맵을 구성
        Dictionary<string, List<int>> groups = new Dictionary<string, List<int>>();
        string filePath = Path.Combine(jsonFolderPath, CardGroupsFileName).Replace("\\", "/");

        // json파일 존재 확인
        if (!File.Exists(filePath)) {
            Debug.LogError($"[CardJSONConverter] cardGroups file not found: {filePath}");
            return groups;
        }

        // jsonFile을 Unity Editor Text 타입으로 로드
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

        // 최상위 루트의 "groups" 속성이 JArray 타입인지 확인
        if (root["groups"] is not JArray groupsArray) {
            Debug.LogWarning($"[CardJSONConverter] Missing 'groups' array in {filePath}");
            return groups;
        }


        foreach (JToken token in groupsArray)
        {
            // token이 JObject 타입인지 확인
            if ((token is not JObject groupObject)) {
                continue;
            }

            // 그룹 식별자(groupName)를 확인
            string groupName = ReadJsonString(groupObject, "groupName");
            // groupName이 비어 있으면 딕셔너리 키로 사용할 수 없으므로 제외
            if (string.IsNullOrWhiteSpace(groupName)) {
                continue;
            }

            // 현재 그룹에 매핑될 카드 ID 목록을 담을 리스트
            List<int> cardIds = new();

            // cardIds 키가 배열인지 확인한 뒤에만 순회
            if (groupObject["cardIds"] is JArray cardIdsArray) {
                foreach (JToken cardIdToken in cardIdsArray) { // cardIds 배열의 각 항목(카드 ID 후보)을 순회
                    int cardId = ReadJsonInt(cardIdToken); // 숫자/문자열 형태 입력을 안전하게 int로 변환
                    if (cardId != 0 && !cardIds.Contains(cardId)) { // 0은 무효 값으로 취급하고, 중복 ID는 한 번만 추가
                        cardIds.Add(cardId); // 검증을 통과한 카드 ID를 리스트에 추가
                    }
                }
            }

            // 최종적으로 groups 딕셔너리에 <groupName, cardIds> 매핑을 저장
            // 동일 groupName이 이미 있으면 최신 값으로 업데이트
            groups[groupName] = cardIds;
        }

        Debug.Log($"[CardJSONConverter] Loaded cardGroups: {groups.Count}");
        return groups;
    }



    // 카드 ID+이름으로 저장 경로를 생성
    // 파일명 불가 문자는 SanitizeFileName에서 치환
    private string BuildCardAssetPath(int cardId, string cardName)
    {
        string rawName = $"{cardId}_{cardName}.asset";
        string safeName = SanitizeFileName(rawName);
        return Path.Combine(outputPath, safeName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string fileName)
    {
        // 운영체제 파일명 금지 문자를 '_'로 치환해 에셋 생성 실패를 방지
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid.ToString(), "_");
        }

        return fileName;
    }


    /// <summary>
    /// CardData SO파일을 모아서 Dictionary를 반환
    /// </summary>
    /// <returns> CardData Dictionary </returns>
    private Dictionary<int, CardData> BuildCardIndexById()
    {
        // Key:cardId, Value:CardData Asset
        Dictionary<int, CardData> map = new();

        // map을 생성하여 outputPath 아래에서 CardData 타입의 GUID(SO파일의 주소) 목록 조회
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { outputPath });

        // 주소를 string으로 변환하여 로드 후 map에 추가
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
    /// CardCollection.asset를 보장(없으면 생성)하여 반환
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
    /// <param name="type"> 이펙트 이름 </param>
    /// <returns> 이펙트 이름에 해당하는 EffectType </returns>
    /// <exception cref="ArgumentException"> type이 null이거나 비어있을 경우 예외를 발생 </exception>
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
            case "DrawSpecific": return EffectType.Draw; // 추후 삭제 or 수정 예정
            case "Buff": return EffectType.Buff; 
            case "Energy": return EffectType.Energy; 
            case "Heal": return EffectType.Heal; 
            case "MultiplyDefense":
            case "MultimediaDefense": return EffectType.MultiplyDefense; // 추후 삭제 or 수정 예정
            case "ConsumeDefense": return EffectType.ConsumeDefense; // 추후 삭제 or 수정 예정
            case "GenerateCard": return EffectType.GenerateCard; // 추후 삭제 or 수정 예정
            case "Keyword": return EffectType.Keyword; // 추후 삭제 or 수정 예정
            case "DiscardHand": return EffectType.DiscardHand; // 추후 삭제 or 수정 예정
            case "ChoiceDiscard": return EffectType.ChoiceDiscard; // 추후 삭제 or 수정 예정
            case "Pickup": return EffectType.Pickup; // 추후 삭제 or 수정 예정
            case "ExhaustHand": return EffectType.ExhaustHand; // 추후 삭제 or 수정 예정
            case "Conditional": return EffectType.Conditional; // 추후 삭제 or 수정 예정
            case "Repeat": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ReduceCost": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ChoiceCard": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Move": return EffectType.Move; // 추후 삭제 or 수정 예정
            case "Copy": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "RandGenerate": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ChoiceGenerate": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ChoiceHand": return EffectType.ChoiceHand;
            case "Discard": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Keep": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Cost": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "CreateCard": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "SelectCard": return EffectType.SelectCard;
            case "Upgrade": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "MixBuff": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ModifyCards": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ModifyCard": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Kill": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ChangeStat": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "ExtraTurn": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Stamina": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "SelectEnemy": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "TransferStats": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "MultiplyBarrier": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Trigger": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "RemoveBuff": return EffectType.Repeat; // 추후 삭제 or 수정 예정
            case "Scry": return EffectType.Scry;
            case "DrawnCard": return EffectType.Repeat; // 추후 삭제 or 수정 예정

            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown effect type: {type}. Fallback to Repeat.");
                return EffectType.Repeat;
        }
    }



    /// <summary>
    /// Parsing TargetType
    /// </summary>
    /// <param name="target"> 타겟 이름 </param>
    /// <returns> 타겟 이름에 해당하는 TargetType 반환 </returns>
    /// <exception cref="ArgumentException"> target이 null이거나 비어있으면 예외 발생 </exception>
    private static TargetType ParseTargetType(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) {
            return TargetType.None;
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
            default:
                Debug.LogWarning($"[CardJSONConverter] Unknown target type: {target}. Fallback to None.");
                return TargetType.None;
        }
    }


    /// <summary>
    /// JObject key 접근용 래퍼
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
    /// String 타입이 아니면 ToString() 결과를 사용
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
    /// JObject key 접근용 래퍼
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
    /// 정수/실수/문자열 숫자를 모두 허용해 int로 변환
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
    /// JObject key 접근용 래퍼
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
    /// 실수/정수/문자열 숫자를 float으로 변환
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

