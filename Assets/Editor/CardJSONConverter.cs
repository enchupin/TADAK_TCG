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

        foreach (string filePath in jsonFiles)
        {
            int count = ConvertFileInternal(filePath, collection);
            totalSuccessCount += count;
        }

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
        int count = ConvertFileInternal(path, collection);
        
        if (count > 0)
        {
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Converted {count} cards from {Path.GetFileName(path)}", "OK");
        }
    }

    int ConvertFileInternal(string path, CardCollection collection)
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
            string fileName = $"{cardJson.cardId}_{cardJson.name}.asset";
            string assetPath = Path.Combine(outputPath, fileName).Replace("\\", "/");

            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            bool isNew = false;

            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<CardData>();
                isNew = true;
            }

            UpdateCardData(cardData, cardJson, jsonFile.text);

            if (isNew) AssetDatabase.CreateAsset(cardData, assetPath);
            else EditorUtility.SetDirty(cardData);

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
                CardEffectData effectData = CreateEffectData(effectJson, rawJson, jsonData.cardId, i);
                if (effectData != null)
                {
                    cardData.effects.Add(effectData);
                }
            }
        }
    }
    
    CardEffectData CreateEffectData(EffectJsonData jsonData, string rawJson, int cardId, int effectIndex)
    {
        CardEffectData effectData = new CardEffectData();
        
        string effectType = jsonData.type;
        
        // amount 필드 수동 파싱 (rawJson에서 직접 추출)
        ParseAmountField(effectData, rawJson, cardId, effectIndex);
        
        // target 필드
        if (!string.IsNullOrEmpty(jsonData.target))
        {
            effectData.target = jsonData.target switch
            {
                "AllEnemies" => TargetType.AllEnemies,
                "SingleEnemy" => TargetType.SingleEnemy,
                "Self" => TargetType.Self,
                _ => TargetType.SingleEnemy
            };
        }
        
        // 효과 타입별 처리
        switch (effectType)
        {
            case "Attack":
            case "Damage":
                effectData.type = EffectType.Damage;
                break;
                
            case "Defense":
                effectData.type = EffectType.Defense;
                break;
                
            case "Draw":
                effectData.type = EffectType.Draw;
                break;
                
            case "Buff":
                effectData.type = EffectType.Buff;
                effectData.stat = jsonData.stat ?? "";
                effectData.duration = jsonData.duration;
                break;
                
            case "Energy":
                effectData.type = EffectType.Energy;
                break;
                
            case "DamagePerCardPlayed":
                effectData.type = EffectType.DamagePerCardPlayed;
                effectData.baseDamage = jsonData.baseDamage;
                effectData.bonusPerCard = jsonData.bonusPerCard;
                break;
                
            case "Execute":
            case "ExecuteDamage":
                effectData.type = EffectType.Execute;
                effectData.baseDamage = jsonData.baseDamage;
                effectData.hpThreshold = jsonData.hpThreshold;
                effectData.multiplier = jsonData.multiplier;
                break;
                
            case "Heal":
                effectData.type = EffectType.Heal;
                break;
                
            case "MultiplyDefense":
                effectData.type = EffectType.MultiplyDefense;
                break;
                
            case "ConsumeDefense":
                effectData.type = EffectType.ConsumeDefense;
                // TODO: 중첩 효과 파싱 (나중에 구현)
                break;
                
            case "GenerateCard":
                effectData.type = EffectType.GenerateCard;
                effectData.RandomCard = jsonData.RandomCard;
                break;
                
            case "Keyword":
                effectData.type = EffectType.Keyword;
                effectData.keyword = jsonData.keyword ?? "";
                break;
                
            case "ChoiceHand":
                effectData.type = EffectType.ChoiceHand;
                // TODO: 구현 예정
                break;
                
            case "ChoiceDiscard":
                effectData.type = EffectType.ChoiceDiscard;
                // TODO: 구현 예정
                break;
                
            case "Conditional":
                effectData.type = EffectType.Conditional;
                // TODO: 구현 예정
                break;
                
            case "RandomGenerate":
                effectData.type = EffectType.RandomGenerate;
                // TODO: 구현 예정
                break;
                
            default:
                Debug.LogWarning($"[Converter] Unknown effect type: {effectType} - 기본 Damage로 처리");
                effectData.type = EffectType.Damage;
                break;
        }
        
        return effectData;
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
