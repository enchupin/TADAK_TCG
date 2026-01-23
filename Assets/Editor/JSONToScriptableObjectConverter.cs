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
    private TextAsset jsonFile;
    private string outputPath = "Assets/Data/Cards";
    private bool createCollection = true;
    
    [MenuItem("Tools/TCG/JSON to ScriptableObject Converter")]
    public static void ShowWindow()
    {
        GetWindow<JSONToScriptableObjectConverter>("Card Converter");
    }
    
    void OnGUI()
    {
        GUILayout.Label("JSON to ScriptableObject Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        createCollection = EditorGUILayout.Toggle("Create Collection", createCollection);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Convert All Cards", GUILayout.Height(30)))
        {
            ConvertJSONToScriptableObjects();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. JSON 파일을 선택하세요\n" +
            "2. 출력 경로를 설정하세요\n" +
            "3. Convert All Cards 버튼을 클릭하세요", 
            MessageType.Info);
    }
    
    void ConvertJSONToScriptableObjects()
    {
        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a JSON file!", "OK");
            return;
        }
        
        // JSON 파싱
        CardDataList cardDataList = JsonUtility.FromJson<CardDataList>(jsonFile.text);
        
        if (cardDataList == null || cardDataList.cards == null)
        {
            EditorUtility.DisplayDialog("Error", "Invalid JSON format!", "OK");
            return;
        }
        
        // 출력 폴더 생성
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        // CardCollection 생성
        CardCollection collection = null;
        if (createCollection)
        {
            collection = ScriptableObject.CreateInstance<CardCollection>();
        }
        
        int successCount = 0;
        
        // 각 카드를 ScriptableObject로 변환
        foreach (var cardJson in cardDataList.cards)
        {
            CardData cardData = CreateCardData(cardJson);
            
            if (cardData != null)
            {
                // 파일로 저장
                string fileName = $"{cardData.cardId}_{cardData.cardName}.asset";
                string assetPath = Path.Combine(outputPath, fileName);
                
                AssetDatabase.CreateAsset(cardData, assetPath);
                
                // Collection에 추가
                if (collection != null)
                {
                    collection.allCards.Add(cardData);
                }
                
                successCount++;
            }
        }
        
        // CardCollection 저장
        if (collection != null)
        {
            string collectionPath = Path.Combine(outputPath, "CardCollection.asset");
            AssetDatabase.CreateAsset(collection, collectionPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", 
            $"Successfully converted {successCount} cards!\nSaved to: {outputPath}", "OK");
    }
    
    CardData CreateCardData(CardJsonData jsonData)
    {
        CardData cardData = ScriptableObject.CreateInstance<CardData>();
        
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
        
        return cardData;
    }
    
    private Character GetCharacterFromId(int characterId)
    {
        switch (characterId)
        {
            case 101: return Character.Warrior;
            case 102: return Character.Mage;
            case 103: return Character.Archer;
            case 104: return Character.Assassin;
            case 105: return Character.Priest;
            case 106: return Character.Knight;
            default: return Character.Warrior;
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
