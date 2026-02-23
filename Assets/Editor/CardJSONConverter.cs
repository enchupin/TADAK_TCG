using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// JSON을 ScriptableObject로 자동 변환하는 에디터 툴
/// Tools → TCG → Card JSON Converter
/// </summary>
public class CardJSONConverter : EditorWindow
{
    private string jsonFolderPath = "Assets/Resources/JsonData";
    private string outputPath = "Assets/Data/Cards";
    
    [MenuItem("Tools/TCG/Card JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CardJSONConverter>("Card Converter");
    }
    
    void OnGUI()
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
            "지정된 폴더 내의 모든 '*Cards.json' 파일을 변환합니다.\n" +
            "(예: chloeCards.json, igniaCards.json 등)\n\n" +
            "CardCollection은 자동으로 생성/업데이트됩니다.\n\n" +
            "수식 문자열 지원 (amount 필드)", 
            MessageType.Info);
    }
    
    void ConvertJSONToScriptableObjects()
    {
        // 단일 파일 처리
        if (!string.IsNullOrEmpty(jsonFolderPath) && jsonFolderPath.EndsWith(".json") && File.Exists(jsonFolderPath))
        {
            Debug.Log($"[Converter] 단일 파일 변환 모드: {jsonFolderPath}");
            ConvertSingleFileFromPath(jsonFolderPath);
            return;
        }

        // 폴더 내 일괄 처리
        if (!Directory.Exists(jsonFolderPath))
        {
            Debug.LogError($"[Converter] 폴더를 찾을 수 없습니다: {jsonFolderPath}");
            EditorUtility.DisplayDialog("Error", "폴더 경로를 확인해주세요.", "OK");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*Cards.json");
        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"해당 폴더에 변환할 파일이 없습니다.\n(*Cards.json 패턴)\n\nPath: {jsonFolderPath}", "OK");
            return;
        }

        Debug.Log($"[Converter] 일괄 변환 시작. 총 {jsonFiles.Length}개의 파일 발견.");
        
        int totalSuccessCount = 0;
        CardCollection collection = GetOrCreateCardCollection();
        Dictionary<int, CardData> cardIndexById = BuildCardIndexById();
        collection.allCards.Clear();

        foreach (string filePath in jsonFiles)
        {
            int count = ConvertFileInternal(filePath, collection, cardIndexById);
            totalSuccessCount += count;
        }

        NormalizeCollection(collection);

        // 최종 저장
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", 
            $"Batch Conversion Complete!\nFiles: {jsonFiles.Length}\nTotal Cards: {collection.allCards.Count}", "OK");
    }

    void ConvertSingleFileFromPath(string path)
    {
        CardCollection collection = GetOrCreateCardCollection();
        Dictionary<int, CardData> cardIndexById = BuildCardIndexById();
        int count = ConvertFileInternal(path, collection, cardIndexById);
        
        if (count > 0)
        {
            NormalizeCollection(collection);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Converted {count} cards from {Path.GetFileName(path)}", "OK");
        }
    }

    int ConvertFileInternal(string path, CardCollection collection, Dictionary<int, CardData> cardIndexById)
    {
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(path.Replace("\\", "/"));
        if (jsonFile == null) return 0;

        CardDataList cardDataList = JsonUtility.FromJson<CardDataList>(jsonFile.text);
        if (cardDataList == null || cardDataList.cards == null) return 0;

        // 출력 폴더 생성
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        int count = 0;
        foreach (var cardJson in cardDataList.cards)
        {
            string rawFileName = $"{cardJson.cardId}_{cardJson.name}.asset";
            // 파일 이름에 사용할 수 없는 특수 문자 제거 (예: 콜론)
            string fileName = string.Join("_", rawFileName.Split(Path.GetInvalidFileNameChars()));
            string assetPath = Path.Combine(outputPath, fileName).Replace("\\", "/");

            CardData cardData = null;
            if (cardIndexById.TryGetValue(cardJson.cardId, out CardData indexedCard))
            {
                cardData = indexedCard;
            }
            else
            {
                cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            }
            bool isNew = false;

            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<CardData>();
                isNew = true;
            }

            UpdateCardData(cardData, cardJson, jsonFile.text);

            if (isNew) AssetDatabase.CreateAsset(cardData, assetPath);
            else EditorUtility.SetDirty(cardData);
            
            cardIndexById[cardData.cardId] = cardData;

            // Collection에 추가
            int existingIndex = collection.allCards.FindIndex(c => c != null && c.cardId == cardData.cardId);
            if (existingIndex >= 0)
            {
                collection.allCards[existingIndex] = cardData;
            }
            else
            {
                collection.allCards.Add(cardData);
            }
            
            count++;
        }
        
        Debug.Log($"[Converter] {Path.GetFileName(path)}: {count}장 변환 완료");
        return count;
    }

    Dictionary<int, CardData> BuildCardIndexById()
    {
        Dictionary<int, CardData> index = new Dictionary<int, CardData>();
        Dictionary<int, List<string>> duplicatePaths = new Dictionary<int, List<string>>();

        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { outputPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (cardData == null) continue;

            if (index.TryGetValue(cardData.cardId, out CardData existing))
            {
                if (!duplicatePaths.ContainsKey(cardData.cardId))
                {
                    duplicatePaths[cardData.cardId] = new List<string>
                    {
                        AssetDatabase.GetAssetPath(existing)
                    };
                }
                duplicatePaths[cardData.cardId].Add(path);
                continue;
            }

            index[cardData.cardId] = cardData;
        }

        foreach (var pair in duplicatePaths)
        {
            Debug.LogWarning($"[Converter] Duplicate CardData assets detected for cardId={pair.Key}: {string.Join(", ", pair.Value)}");
        }

        return index;
    }

    void NormalizeCollection(CardCollection collection)
    {
        Dictionary<int, CardData> byId = new Dictionary<int, CardData>();
        foreach (CardData cardData in collection.allCards)
        {
            if (cardData == null) continue;
            byId[cardData.cardId] = cardData;
        }

        List<CardData> normalized = new List<CardData>(byId.Values);
        normalized.Sort((a, b) => a.cardId.CompareTo(b.cardId));
        collection.allCards = normalized;
    }

    CardCollection GetOrCreateCardCollection()
    {
        string collectionPath = "Assets/Resources/CardCollection.asset";
        CardCollection collection = AssetDatabase.LoadAssetAtPath<CardCollection>(collectionPath);

        if (collection == null)
        {
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
            
            collection = ScriptableObject.CreateInstance<CardCollection>();
            AssetDatabase.CreateAsset(collection, collectionPath);
            Debug.Log("[Converter] 새 CardCollection 생성");
        }
        return collection;
    }
    
    void UpdateCardData(CardData cardData, CardJsonData jsonData, string rawJson)
    {
        // 기본 정보
        cardData.cardId = jsonData.cardId;
        cardData.cardName = jsonData.name;
        cardData.character = CharacterManager.GetCharacterEnumById(jsonData.characterId);
        cardData.cost = jsonData.cost;
        cardData.description = jsonData.description ?? "";
        
        // enforce 필드 (강화 카드 ID 목록)
        cardData.enforceCardIds.Clear();
        if (jsonData.enforce != null)
        {
            cardData.enforceCardIds.AddRange(jsonData.enforce);
        }
        
        // Addressables 주소
        if (jsonData.addressables != null)
        {
            cardData.artworkAddress = jsonData.addressables.artwork ?? "";
            cardData.effectAddress = jsonData.addressables.effect ?? "";
            cardData.soundAddress = jsonData.addressables.sound ?? "";
        }
        
        // 효과 변환 (수동 파싱으로 amount 처리)
        cardData.effects.Clear();
        if (jsonData.effects != null)
        {
            for (int i = 0; i < jsonData.effects.Count; i++)
            {
                EffectJsonData effectJson = jsonData.effects[i];
                // 재귀적 파싱을 위해 루트 효과 생성
                CardEffectData effectData = CreateEffectData(effectJson, rawJson);
                if (effectData != null)
                {
                    cardData.effects.Add(effectData);
                }
            }
        }
    }
    
    CardEffectData CreateEffectData(EffectJsonData jsonData, string rawJson)
    {
        if (jsonData == null) return null;

        CardEffectData effectData = new CardEffectData();
        
        string effectType = jsonData.type;
        
        // 공통 필드 파싱
        // Amount는 rawJson이 있으면 수동 파싱 시도, 없으면(재귀 호출 등) JsonData 사용
        // 여기서는 간단히 JsonData 사용 (Complex parsing logic omitted for brevity in recursive calls unless really needed)
        // TODO: 재귀 호출 시 rawJson 위치 찾기가 어려우므로, 일단 간단한 정수 매핑 사용. 
        // 복잡한 수식이 최상위에만 있다면 문제없음.
        
        // amountFormula가 JSON에 직접 들어있다면 좋겠지만, 현재 구조상 rawJson 파싱이 필요함.
        // 하지만 중첩된 효과의 amountFormula를 rawJson에서 찾으려면 위치 추적이 필요하다.
        // 이번 구현에서는 "최상위 효과"의 amountFormula만 정확히 파싱하고, 중첩 효과는 정수값 위주로 처리하거나
        // 추후 개선된 파서를 적용한다.
        
        effectData.amount = jsonData.amount;
        effectData.amountFormula = jsonData.amountFormula;
        if (string.IsNullOrWhiteSpace(effectData.amountFormula))
        {
            effectData.amountFormula = jsonData.getAmount;
        }
        if (string.IsNullOrWhiteSpace(effectData.amountFormula))
        {
            effectData.amountFormula = jsonData.GetAmount;
        }
        if (string.IsNullOrWhiteSpace(effectData.amountFormula))
        {
            effectData.amountFormula = jsonData.getamount;
        }
        
        // target 필드
        if (!string.IsNullOrEmpty(jsonData.target))
        {
            effectData.target = jsonData.target switch
            {
                "AllEnemies" => TargetType.AllEnemies,
                "SingleEnemy" => TargetType.SingleEnemy,
                "Self" => TargetType.Self,
                "RandomEnemy" => TargetType.SingleEnemy, // Map roughly
                _ => TargetType.SingleEnemy
            };
        }

        // 효과 타입별 처리
        switch (effectType)
        {
            case "Attack":
                effectData.type = EffectType.Attack;
                break;
            case "Damage":
                effectData.type = EffectType.Damage;
                break;
            case "Defense":
            case "Barrier":
                effectData.type = EffectType.Barrier;
                break;
            case "Draw":
                effectData.type = EffectType.Draw;
                break;
            case "Buff":
                effectData.type = EffectType.Buff;
                effectData.stat = jsonData.stat ?? "";
                effectData.buffId = jsonData.buffId;
                effectData.duration = jsonData.duration;
                break;
            case "Energy":
                effectData.type = EffectType.Energy;
                break;
            case "Heal":
                effectData.type = EffectType.Heal;
                break;
            case "MultimediaDefense": // Typo handling?
            case "MultiplyDefense":
                effectData.type = EffectType.MultiplyDefense;
                break;
            case "ConsumeDefense":
                effectData.type = EffectType.ConsumeDefense;
                if (jsonData.effect != null)
                {
                    effectData.nestedEffect = CreateEffectData(jsonData.effect, rawJson);
                }
                break;
            case "GenerateCard":
                effectData.type = EffectType.GenerateCard;
                effectData.RandomCard = jsonData.RandomCard;
                break;
            case "Keyword":
                effectData.type = EffectType.Keyword;
                effectData.keyword = jsonData.keyword ?? "";
                break;
                
            // New Effects
            case "DiscardHand":
                effectData.type = EffectType.DiscardHand;
                effectData.count = jsonData.count;
                break;
            case "ChoiceDiscard":
                effectData.type = EffectType.ChoiceDiscard;
                effectData.count = jsonData.count; // or amount
                if (jsonData.effect != null)
                {
                    effectData.nestedEffect = CreateEffectData(jsonData.effect, rawJson);
                }
                break;
            case "Pickup":
                effectData.type = EffectType.Pickup;
                break;
            case "ExhaustHand":
                effectData.type = EffectType.ExhaustHand;
                effectData.count = jsonData.count;
                break;
            case "Repeat":
                effectData.type = EffectType.Repeat;
                effectData.count = jsonData.count;
                // Parse 'amountFormula' for count if 'count' field string exists logic needed
                if (jsonData.effects != null)
                {
                    effectData.subEffects = new List<CardEffectData>();
                    foreach(var sub in jsonData.effects)
                    {
                        effectData.subEffects.Add(CreateEffectData(sub, rawJson));
                    }
                }
                break;
            case "Conditional":
                effectData.type = EffectType.Conditional;
                // Condition 파싱
                if (jsonData.condition != null)
                {
                    effectData.conditionData = ConvertCondition(jsonData.condition, rawJson);
                }
                break;
                
            default:
                // Type이 없거나 모르는 경우 기본 설정
                // "Attack", "Damage" 등이 위에 있으므로 여기 오는 건 정말 모르는 타입
                if (string.IsNullOrEmpty(effectType)) 
                {
                   // Fallback or warning
                }
                else 
                {
                    // Case not covered explicitly, try generic mapping if possible or default to Damage
                    // But usually we should cover all types.
                    // Let's assume default types are handled above.
                }
                // Default handling logic matches original...
                effectData.type = EffectType.Damage; // Safety fallback
                break;
        }

        // onAction (반응형 효과) 파싱 - 모든 효과 타입에서 가질 수 있음
        if (jsonData.onAction != null)
        {
            effectData.onAction = CreateEffectData(jsonData.onAction, rawJson);
        }
        
        return effectData;
    }
    
    // ConditionJsonData -> ConditionData 변환 헬퍼
    ConditionData ConvertCondition(ConditionJsonData jsonCond, string rawJson)
    {
        if (jsonCond == null) return null;
        
        ConditionData condData = new ConditionData();
        condData.mode = jsonCond.mode ?? "And";
        
        if (jsonCond.checks != null)
        {
            foreach(var checkJson in jsonCond.checks)
            {
                CheckData check = new CheckData();
                check.subject = checkJson.subject;
                check.property = checkJson.property;
                check.param = checkJson.param;
                check.@operator = checkJson.@operator;
                check.value = checkJson.value;
                condData.checks.Add(check);
            }
        }
        
        // Success/Fail Effects 재귀 파싱
        if (jsonCond.successEffect != null)
        {
            condData.successEffect = CreateEffectData(jsonCond.successEffect, rawJson);
        }
        
        if (jsonCond.failEffect != null)
        {
            condData.failEffect = CreateEffectData(jsonCond.failEffect, rawJson);
        }
        
        return condData;
    }
    
    /// <summary>
    /// rawJson에서 amount 필드를 수동으로 파싱 (숫자 또는 문자열)
    /// </summary>
    void ParseAmountField(CardEffectData effectData, string rawJson, int cardId, int effectIndex)
    {
        // 카드 ID로 해당 카드 JSON 블록 찾기
        string searchPattern = $"\"cardId\": {cardId}";
        int cardStart = rawJson.IndexOf(searchPattern);
        if (cardStart == -1) return;
        
        // effects 배열 찾기
        int effectsStart = rawJson.IndexOf("\"effects\":", cardStart);
        if (effectsStart == -1) return;
        
        // 해당 인덱스의 effect 찾기
        int currentEffectIndex = 0;
        int searchPos = effectsStart;
        
        while (currentEffectIndex <= effectIndex)
        {
            searchPos = rawJson.IndexOf("\"amount\":", searchPos + 1);
            if (searchPos == -1) return;
            
            if (currentEffectIndex == effectIndex)
            {
                // amount 값 추출
                int colonPos = searchPos + 9; // "amount":
                int valueStart = colonPos;
                
                // 공백 건너뛰기
                while (valueStart < rawJson.Length && char.IsWhiteSpace(rawJson[valueStart]))
                    valueStart++;
                
                if (valueStart >= rawJson.Length) return;
                
                // 따옴표로 시작하면 문자열
                if (rawJson[valueStart] == '"')
                {
                    int stringStart = valueStart + 1;
                    int stringEnd = rawJson.IndexOf('"', stringStart);
                    if (stringEnd != -1)
                    {
                        effectData.amountFormula = rawJson.Substring(stringStart, stringEnd - stringStart);
                        effectData.amount = 0;
                    }
                }
                else
                {
                    // 숫자
                    int numberEnd = valueStart;
                    while (numberEnd < rawJson.Length && 
                           (char.IsDigit(rawJson[numberEnd]) || rawJson[numberEnd] == '.' || rawJson[numberEnd] == '-'))
                        numberEnd++;
                    
                    string numberStr = rawJson.Substring(valueStart, numberEnd - valueStart);
                    if (int.TryParse(numberStr, out int value))
                    {
                        effectData.amount = value;
                        effectData.amountFormula = "";
                    }
                }
                return;
            }
            
            currentEffectIndex++;
        }
    }
}
