using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Character JSON을 ScriptableObject로 자동 변환하는 에디터 툴
/// Tools → TCG → Character JSON Converter
/// </summary>
public class CharacterJSONConverter : EditorWindow
{
    private string jsonFilePath = "Assets/Resources/JsonData/characters.json";
    private string outputPath = "Assets/Data/Character";
    
    [MenuItem("Tools/TCG/Character JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CharacterJSONConverter>("Character Converter");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Character JSON to ScriptableObject Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        jsonFilePath = EditorGUILayout.TextField("JSON File Path", jsonFilePath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Convert All Characters", GUILayout.Height(30)))
        {
            ConvertCharactersToScriptableObjects();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. JSON 파일 경로를 확인하세요\n" +
            "2. 출력 경로를 설정하세요\n" +
            "3. Convert All Characters 버튼을 클릭하세요", 
            MessageType.Info);
    }
    
    void ConvertCharactersToScriptableObjects()
    {
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonFilePath);

        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not find JSON file at {jsonFilePath}", "OK");
            return;
        }
        
        // JSON 파싱
        CharacterJsonRoot jsonRoot = JsonUtility.FromJson<CharacterJsonRoot>(jsonFile.text);
        
        if (jsonRoot == null || jsonRoot.characters == null)
        {
            EditorUtility.DisplayDialog("Error", "Invalid JSON format!", "OK");
            return;
        }
        
        // 출력 폴더 생성
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        int successCount = 0;
        int failCount = 0;
        
        // 각 캐릭터를 ScriptableObject로 변환
        foreach (var jsonChar in jsonRoot.characters)
        {
            try
            {
                // 파일 이름 생성
                string fileName = $"Character_{jsonChar.characterId}_{jsonChar.name}.asset";
                string assetPath = Path.Combine(outputPath, fileName).Replace("\\", "/");
                
                // 이미 존재하는 에셋인지 확인
                CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
                bool isNew = false;
                
                if (characterData == null)
                {
                    characterData = ScriptableObject.CreateInstance<CharacterData>();
                    isNew = true;
                }
                
                // 데이터 업데이트
                UpdateCharacterData(characterData, jsonChar);
                
                if (isNew)
                {
                    AssetDatabase.CreateAsset(characterData, assetPath);
                    Debug.Log($"Created: {assetPath}");
                }
                else
                {
                    EditorUtility.SetDirty(characterData);
                    Debug.Log($"Updated: {assetPath}");
                }
                
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create character {jsonChar.characterId} ({jsonChar.name}): {e.Message}");
                failCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"Successfully converted {successCount} characters!\n";
        if (failCount > 0)
        {
            message += $"Failed: {failCount}\n";
        }
        message += $"Saved to: {outputPath}";
        
        EditorUtility.DisplayDialog("Conversion Complete", message, "OK");
        Debug.Log($"<color=green>Character conversion completed!</color> Success: {successCount}, Failed: {failCount}");
    }
    
    void UpdateCharacterData(CharacterData data, CharacterJsonData jsonChar)
    {
        // 기본 정보
        data.characterId = jsonChar.characterId;
        data.characterName = jsonChar.name;
        data.maxHp = jsonChar.maxHp;
        data.cost = jsonChar.cost;
        data.characterColor = jsonChar.characterColor;
        
        // Addressables
        if (jsonChar.addressables != null)
        {
            data.artworkAddress = jsonChar.addressables.artwork ?? "";
            data.effectAddress = jsonChar.addressables.effect ?? "";
            data.soundAddress = jsonChar.addressables.sound ?? "";
        }
        else
        {
            data.artworkAddress = "";
            data.effectAddress = "";
            data.soundAddress = "";
        }
        
        // 시작 덱
        if (jsonChar.startDeck != null)
        {
            data.startDeckCardIds = new List<int>(jsonChar.startDeck);
        }
        else
        {
            data.startDeckCardIds = new List<int>();
        }
        
        // Identity 스킬
        if (jsonChar.identity != null && jsonChar.identity.effects != null && jsonChar.identity.effects.Count > 0)
        {
            data.identity = new IdentitySkillData
            {
                cost = jsonChar.identity.cost,
                description = jsonChar.identity.description ?? "",
                effects = new List<IdentityEffectData>()
            };
            
            foreach (var jsonEffect in jsonChar.identity.effects)
            {
                IdentityEffectData effectData = new IdentityEffectData
                {
                    type = ParseIdentityEffectType(jsonEffect.type),
                    amount = jsonEffect.amount,
                    target = ParseIdentityTargetType(jsonEffect.target),
                    buffType = jsonEffect.buffType ?? "",
                    cardId = jsonEffect.cardId
                };
                
                data.identity.effects.Add(effectData);
            }
        }
        else
        {
            // Identity가 없는 경우 null로 설정
            data.identity = null;
        }
    }
    
    private IdentityEffectType ParseIdentityEffectType(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning("Effect type is null or empty, defaulting to Buff");
            return IdentityEffectType.Buff;
        }
        
        switch (type)
        {
            case "Buff": return IdentityEffectType.Buff;
            case "Draw": return IdentityEffectType.Draw;
            case "GenerateCard": return IdentityEffectType.GenerateCard;
            case "Damage": return IdentityEffectType.Damage;
            case "Heal": return IdentityEffectType.Heal;
            default:
                Debug.LogWarning($"Unknown identity effect type: {type}, defaulting to Buff");
                return IdentityEffectType.Buff;
        }
    }
    
    private IdentityTargetType ParseIdentityTargetType(string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("Target type is null or empty, defaulting to Self");
            return IdentityTargetType.Self;
        }
        
        switch (target)
        {
            case "Self": return IdentityTargetType.Self;
            case "AllEnemies": return IdentityTargetType.AllEnemies;
            case "RandomEnemy": return IdentityTargetType.RandomEnemy;
            case "Hand": return IdentityTargetType.Hand;
            default:
                Debug.LogWarning($"Unknown identity target type: {target}, defaulting to Self");
                return IdentityTargetType.Self;
        }
    }
}

// ==================== Character JSON Data Classes ====================

/// <summary>
/// JSON 파싱용 루트 클래스
/// </summary>
[System.Serializable]
public class CharacterJsonRoot
{
    public List<CharacterJsonData> characters;
}

/// <summary>
/// JSON 파싱용 캐릭터 데이터
/// </summary>
[System.Serializable]
public class CharacterJsonData
{
    public int characterId;
    public string name;
    public int maxHp;
    public int cost;
    public CharacterIdentityJsonData identity;
    public CharacterAddressablesJsonData addressables;
    public string characterColor;
    public List<int> startDeck;
}

/// <summary>
/// JSON 파싱용 Identity 데이터
/// </summary>
[System.Serializable]
public class CharacterIdentityJsonData
{
    public int cost;
    public List<CharacterEffectJsonData> effects;
    public string description;
}

/// <summary>
/// JSON 파싱용 효과 데이터
/// </summary>
[System.Serializable]
public class CharacterEffectJsonData
{
    public string type;
    public string buffType;
    public int amount;
    public string target;
    public int cardId;
}

/// <summary>
/// JSON 파싱용 Addressables 데이터
/// </summary>
[System.Serializable]
public class CharacterAddressablesJsonData
{
    public string artwork;
    public string effect;
    public string sound;
}
