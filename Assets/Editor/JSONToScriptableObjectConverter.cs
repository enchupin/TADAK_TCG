using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// JSON을 ScriptableObject로 자동 변환하는 에디터 툴
/// Tools → TCG → JSON to ScriptableObject Converter
/// </summary>
public class JSONToScriptableObjectConverter : EditorWindow
{
    private string jsonFolderPath = "Assets/Resources/JsonData"; // 기본값: 폴더 경로
    private string outputPath = "Assets/Data/Cards";
    
    [MenuItem("Tools/TCG/JSON to ScriptableObject Converter")]
    public static void ShowWindow()
    {
        GetWindow<JSONToScriptableObjectConverter>("Card Converter");
    }
    
    void OnGUI()
    {
        GUILayout.Label("JSON Batch Converter", EditorStyles.boldLabel);
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
            "CardCollection은 자동으로 생성/업데이트됩니다.", 
            MessageType.Info);
    }
    
    void ConvertJSONToScriptableObjects()
    {
        // 1. 단일 파일 처리 지원 (입력된 경로가 .json으로 끝나는 경우)
        if (!string.IsNullOrEmpty(jsonFolderPath) && jsonFolderPath.EndsWith(".json") && File.Exists(jsonFolderPath))
        {
            Debug.Log($"[Converter] 단일 파일 변환 모드: {jsonFolderPath}");
            ConvertSingleFileFromPath(jsonFolderPath);
            return;
        }

        // 2. 폴더 내 일괄 처리
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
        
        // 기존 데이터 삭제 (Clean Build) - 필요 시 주석 해제하여 사용
        // ClearOutputFolder(); 

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

    /// <summary>
    /// 단일 파일 변환 (외부 호출 또는 버튼 클릭)
    /// </summary>
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

    /// <summary>
    /// 내부 변환 로직 (파일 1개 -> SO 변환 -> Collection 추가)
    /// </summary>
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

            UpdateCardData(cardData, cardJson);

            if (isNew) AssetDatabase.CreateAsset(cardData, assetPath);
            else EditorUtility.SetDirty(cardData);

            // Collection에 추가 (중복 방지: 동일 ID의 기존 카드가 있으면 제거하고 새 것으로 교체)
            // 주의: List.Contains는 참조 비교이므로, ID 기반으로 찾아야 함
            int existingIndex = collection.allCards.FindIndex(c => c != null && c.cardId == cardData.cardId);
            if (existingIndex >= 0)
            {
                collection.allCards[existingIndex] = cardData; // 갱신
            }
            else
            {
                collection.allCards.Add(cardData); // 신규 추가
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
    
    void UpdateCardData(CardData cardData, CardJsonData jsonData)
    {
        // 기본 정보
        cardData.cardId = jsonData.cardId;
        cardData.cardName = jsonData.name;
        cardData.character = GetCharacterFromId(jsonData.characterId);
        cardData.cost = jsonData.cost;
        cardData.rarity = jsonData.rarity;
        
        // Addressables 주소
        if (jsonData.addressables != null)
        {
            cardData.artworkAddress = jsonData.addressables.artwork;
            cardData.effectAddress = jsonData.addressables.effect;
            cardData.soundAddress = jsonData.addressables.sound;
        }
        
        // 효과 변환
        cardData.effects.Clear(); // 줄 바뀐 효과나 삭제된 효과 반영을 위해 초기화
        if (jsonData.effects != null)
        {
            foreach (var effectJson in jsonData.effects)
            {
                CardEffectData effectData = CreateEffectData(effectJson);
                if (effectData != null)
                {
                    cardData.effects.Add(effectData);
                }
            }
        }
    }
    
    private Character GetCharacterFromId(int characterId)
    {
        switch (characterId)
        {
            case 101: return Character.Chloe;
            case 201: return Character.Ignia;
            case 301: return Character.Declan;
            case 102: return Character.Mage;
            case 103: return Character.Archer;
            case 104: return Character.Assassin;
            case 105: return Character.Priest;
            case 106: return Character.Knight;
            default: return Character.Chloe;
        }
    }
    
    CardEffectData CreateEffectData(EffectJsonData jsonData)
    {
        CardEffectData effectData = new CardEffectData();
        
        // 효과 타입 변환
        switch (jsonData.type)
        {
            case "Damage":
                effectData.type = EffectType.Damage;
                effectData.amount = jsonData.amount;
                effectData.target = jsonData.target == "AllEnemies" ? 
                    TargetType.AllEnemies : TargetType.SingleEnemy;
                break;
                
            case "Defense":
                effectData.type = EffectType.Defense;
                effectData.amount = jsonData.amount;
                break;
                
            case "Draw":
                effectData.type = EffectType.Draw;
                effectData.amount = jsonData.amount;
                break;
                
            case "Buff":
                effectData.type = EffectType.Buff;
                effectData.stat = jsonData.stat;
                effectData.amount = jsonData.amount;
                effectData.duration = jsonData.duration;
                break;
                
            case "Energy":
                effectData.type = EffectType.Energy;
                effectData.amount = jsonData.amount;
                break;
                
            case "DamagePerCardPlayed":
                effectData.type = EffectType.DamagePerCardPlayed;
                effectData.baseDamage = jsonData.baseDamage;
                effectData.bonusPerCard = jsonData.bonusPerCard;
                break;
                
            case "ExecuteDamage":
                effectData.type = EffectType.Execute;
                effectData.baseDamage = jsonData.baseDamage;
                effectData.hpThreshold = jsonData.hpThreshold;
                effectData.multiplier = jsonData.multiplier;
                break;
                
            default:
                Debug.LogWarning($"Unknown effect type: {jsonData.type}");
                return null;
        }
        
        return effectData;
    }
}
