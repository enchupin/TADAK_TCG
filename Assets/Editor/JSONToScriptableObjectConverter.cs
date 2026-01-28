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
    private string jsonFilePath = "Assets/Resources/JsonData/chloeCards.json";
    private string outputPath = "Assets/Data/Cards";
    
    [MenuItem("Tools/TCG/JSON to ScriptableObject Converter")]
    public static void ShowWindow()
    {
        GetWindow<JSONToScriptableObjectConverter>("Card Converter");
    }
    
    void OnGUI()
    {
        GUILayout.Label("JSON to ScriptableObject Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        jsonFilePath = EditorGUILayout.TextField("JSON File Path", jsonFilePath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Convert All Cards", GUILayout.Height(30)))
        {
            ConvertJSONToScriptableObjects();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. JSON 파일 경로를 확인하세요\n" +
            "2. 출력 경로를 설정하세요\n" +
            "3. Convert All Cards 버튼을 클릭하세요\n\n" +
            "CardCollection은 자동으로 생성/업데이트됩니다.", 
            MessageType.Info);
    }
    
    void ConvertJSONToScriptableObjects()
    {
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonFilePath);

        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not find JSON file at {jsonFilePath}", "OK");
            return;
        }
        
        // JSON 파싱
        CardDataList cardDataList = JsonUtility.FromJson<CardDataList>(jsonFile.text);
        
        if (cardDataList == null || cardDataList.cards == null)
        {
            EditorUtility.DisplayDialog("Error", "Invalid JSON format!", "OK");
            return;
        }
        
        // ✅ 기존 CardData SO 파일들 모두 삭제
        if (Directory.Exists(outputPath))
        {
            // outputPath의 모든 .asset 파일 찾기
            string[] existingAssets = Directory.GetFiles(outputPath, "*.asset", SearchOption.TopDirectoryOnly);
            
            int deletedCount = 0;
            foreach (string assetPath in existingAssets)
            {
                // CardCollection.asset은 제외 (나중에 업데이트)
                if (!assetPath.Contains("CardCollection.asset"))
                {
                    AssetDatabase.DeleteAsset(assetPath.Replace("\\", "/"));
                    deletedCount++;
                }
            }
            
            Debug.Log($"[Converter] 기존 CardData SO {deletedCount}개 삭제 완료");
        }
        else
        {
            // 출력 폴더 생성
            Directory.CreateDirectory(outputPath);
        }
        
        // ✅ CardCollection 항상 생성/업데이트
        Debug.Log("[Converter] CardCollection 생성/업데이트 시작");
        
        // 기존 CardCollection 로드 시도
        string collectionPath = "Assets/Resources/CardCollection.asset";
        CardCollection collection = AssetDatabase.LoadAssetAtPath<CardCollection>(collectionPath);
        
        if (collection == null)
        {
            Debug.Log($"[Converter] CardCollection이 {collectionPath}에 없음. 새로 생성합니다.");
            
            // Resources 폴더 확인
            if (!System.IO.Directory.Exists("Assets/Resources"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.Refresh();
                Debug.Log("[Converter] Assets/Resources 폴더 생성");
            }
            
            // 없으면 새로 생성
            collection = ScriptableObject.CreateInstance<CardCollection>();
            AssetDatabase.CreateAsset(collection, collectionPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();  // ✅ Refresh 추가!
            
            // 다시 로드하여 확인
            collection = AssetDatabase.LoadAssetAtPath<CardCollection>(collectionPath);
            if (collection != null)
            {
                Debug.Log($"[Converter] 새 CardCollection 생성 완료: {collectionPath}");
            }
            else
            {
                Debug.LogError($"[Converter] CardCollection 생성 실패! {collectionPath}");
            }
        }
        else
        {
            // 기존 컬렉션 초기화
            Debug.Log($"[Converter] 기존 CardCollection 발견: {collectionPath}, 현재 카드 수: {collection.allCards.Count}");
            collection.allCards.Clear();
            Debug.Log("[Converter] 기존 CardCollection 초기화 완료");
        }
        
        int successCount = 0;
        List<string> createdCardPaths = new List<string>();
        
        // 각 카드를 ScriptableObject로 변환
        foreach (var cardJson in cardDataList.cards)
        {
            // 파일 이름 생성
            string fileName = $"{cardJson.cardId}_{cardJson.name}.asset";
            // 특수문자 제거나 안전한 파일명 처리 로직이 있다면 여기에 추가
            
            string assetPath = Path.Combine(outputPath, fileName).Replace("\\", "/");
            
            // 이미 존재하는 에셋인지 확인
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            bool isNew = false;
            
            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<CardData>();
                isNew = true;
            }
            
            // 데이터 업데이트
            UpdateCardData(cardData, cardJson);
            
            if (isNew)
            {
                AssetDatabase.CreateAsset(cardData, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(cardData);
            }

            // 생성된 카드 경로 저장
            createdCardPaths.Add(assetPath);
            
            successCount++;
        }
        
        // ✅ 먼저 모든 CardData 저장
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // ✅ 저장된 CardData를 다시 로드하여 CardCollection에 추가
        foreach (string cardPath in createdCardPaths)
        {
            CardData savedCard = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
            if (savedCard != null)
            {
                collection.allCards.Add(savedCard);
            }
        }
        
        EditorUtility.SetDirty(collection);
        Debug.Log($"[Converter] CardCollection에 {collection.allCards.Count}장의 카드 추가 완료");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", 
            $"Successfully converted {successCount} cards!\nCardCollection: {collection.allCards.Count} cards\nSaved to: {outputPath}", "OK");
    }
    
    void UpdateCardData(CardData cardData, CardJsonData jsonData)
    {
        // 기본 정보
        cardData.cardId = jsonData.cardId;
        cardData.cardName = jsonData.name;
        cardData.character = CharacterManager.GetCharacterEnumById(jsonData.characterId);
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
