using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Character JSON to ScriptableObject converter.
/// </summary>
public class CharacterJSONConverter : EditorWindow
{
    private const string DefaultJsonFilePath = "Assets/Resources/JsonData/characters.json";
    private const string DefaultOutputPath = "Assets/Data/Character";
    private const string CharacterCollectionPath = "Assets/Resources/CharacterCollection.asset";

    private string jsonFilePath = DefaultJsonFilePath;
    private string outputPath = DefaultOutputPath;

    [MenuItem("Tools/TCG/Character JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CharacterJSONConverter>("Character Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Character JSON Converter", EditorStyles.boldLabel);
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
            "Converts characters.json to CharacterData assets and updates CharacterCollection.",
            MessageType.Info);
    }

    private void ConvertCharactersToScriptableObjects()
    {
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonFilePath);
        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not find JSON file: {jsonFilePath}", "OK");
            return;
        }

        CharacterJsonRoot root;
        try
        {
            root = JsonUtility.FromJson<CharacterJsonRoot>(jsonFile.text);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", $"Invalid JSON: {ex.Message}", "OK");
            return;
        }

        if (root?.characters == null)
        {
            EditorUtility.DisplayDialog("Error", "JSON does not contain a valid 'characters' array.", "OK");
            return;
        }

        EnsureDirectory(outputPath);
        CharacterCollection collection = GetOrCreateCollection();
        Dictionary<int, CharacterData> existingById = BuildIndexById();
        Dictionary<int, CharacterData> updatedById = new Dictionary<int, CharacterData>();

        int successCount = 0;
        int failCount = 0;

        foreach (CharacterJsonData jsonCharacter in root.characters)
        {
            if (jsonCharacter == null || jsonCharacter.characterId <= 0)
            {
                failCount++;
                continue;
            }

            try
            {
                CharacterData data = GetOrCreateCharacterData(jsonCharacter, existingById);
                ApplyCharacterData(data, jsonCharacter);

                if (!AssetDatabase.Contains(data))
                {
                    AssetDatabase.CreateAsset(data, BuildCharacterAssetPath(data));
                }
                else
                {
                    EditorUtility.SetDirty(data);
                }

                existingById[data.characterId] = data;
                updatedById[data.characterId] = data;
                successCount++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterJSONConverter] Failed characterId={jsonCharacter.characterId}: {ex.Message}");
                failCount++;
            }
        }

        List<CharacterData> normalized = new List<CharacterData>(updatedById.Values);
        normalized.Sort((a, b) => a.characterId.CompareTo(b.characterId));
        collection.allCharacters = normalized;

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Conversion Complete",
            $"Success: {successCount}\nFailed: {failCount}\nCollection: {collection.allCharacters.Count}",
            "OK");
    }

    private CharacterData GetOrCreateCharacterData(CharacterJsonData jsonCharacter, Dictionary<int, CharacterData> existingById)
    {
        if (existingById.TryGetValue(jsonCharacter.characterId, out CharacterData existing) && existing != null)
        {
            return existing;
        }

        string expectedPath = BuildCharacterAssetPath(jsonCharacter.characterId, jsonCharacter.name);
        CharacterData byPath = AssetDatabase.LoadAssetAtPath<CharacterData>(expectedPath);
        if (byPath != null)
        {
            return byPath;
        }

        return ScriptableObject.CreateInstance<CharacterData>();
    }

    private void ApplyCharacterData(CharacterData data, CharacterJsonData jsonCharacter)
    {
        data.characterId = jsonCharacter.characterId;
        data.characterName = jsonCharacter.name ?? string.Empty;
        data.maxHp = jsonCharacter.maxHp;
        data.cost = jsonCharacter.cost;
        data.characterColor = jsonCharacter.characterColor ?? string.Empty;

        data.artworkAddress = jsonCharacter.addressables?.artwork ?? string.Empty;
        data.effectAddress = jsonCharacter.addressables?.effect ?? string.Empty;
        data.soundAddress = jsonCharacter.addressables?.sound ?? string.Empty;

        data.startDeckCardIds = jsonCharacter.startDeck != null
            ? new List<int>(jsonCharacter.startDeck)
            : new List<int>();

        data.identity = BuildIdentity(jsonCharacter.identity);
    }

    private static IdentitySkillData BuildIdentity(CharacterIdentityJsonData identityJson)
    {
        if (identityJson == null || identityJson.effects == null || identityJson.effects.Count == 0)
        {
            return null;
        }

        IdentitySkillData identity = new IdentitySkillData
        {
            cost = identityJson.cost,
            description = identityJson.description ?? string.Empty,
            effects = new List<IdentityEffectData>()
        };

        foreach (CharacterEffectJsonData effectJson in identityJson.effects)
        {
            if (effectJson == null)
            {
                continue;
            }

            identity.effects.Add(new IdentityEffectData
            {
                type = ParseIdentityEffectType(effectJson.type),
                amount = effectJson.amount,
                target = ParseIdentityTargetType(effectJson.target),
                cardId = effectJson.cardId
            });
        }

        return identity;
    }

    private Dictionary<int, CharacterData> BuildIndexById()
    {
        Dictionary<int, CharacterData> map = new Dictionary<int, CharacterData>();
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { outputPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data == null || data.characterId <= 0)
            {
                continue;
            }

            map[data.characterId] = data;
        }

        return map;
    }

    private CharacterCollection GetOrCreateCollection()
    {
        EnsureDirectory("Assets/Resources");

        CharacterCollection collection = AssetDatabase.LoadAssetAtPath<CharacterCollection>(CharacterCollectionPath);
        if (collection != null)
        {
            return collection;
        }

        collection = ScriptableObject.CreateInstance<CharacterCollection>();
        AssetDatabase.CreateAsset(collection, CharacterCollectionPath);
        return collection;
    }

    private string BuildCharacterAssetPath(CharacterData data)
    {
        return BuildCharacterAssetPath(data.characterId, data.characterName);
    }

    private string BuildCharacterAssetPath(int characterId, string characterName)
    {
        string fileName = SanitizeFileName($"Character_{characterId}_{characterName}.asset");
        return Path.Combine(outputPath, fileName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid.ToString(), "_");
        }

        return name;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static IdentityEffectType ParseIdentityEffectType(string type)
    {
        switch (type)
        {
            case "Draw": return IdentityEffectType.Draw;
            case "GenerateCard": return IdentityEffectType.GenerateCard;
            case "Damage": return IdentityEffectType.Damage;
            case "Heal": return IdentityEffectType.Heal;
            case "Buff":
            default: return IdentityEffectType.Buff;
        }
    }

    private static IdentityTargetType ParseIdentityTargetType(string target)
    {
        switch (target)
        {
            case "AllEnemies": return IdentityTargetType.AllEnemies;
            case "RandomEnemy": return IdentityTargetType.RandomEnemy;
            case "Hand": return IdentityTargetType.Hand;
            case "Self":
            default: return IdentityTargetType.Self;
        }
    }
}

[Serializable]
public class CharacterJsonRoot
{
    public List<CharacterJsonData> characters;
}

[Serializable]
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

[Serializable]
public class CharacterIdentityJsonData
{
    public int cost;
    public List<CharacterEffectJsonData> effects;
    public string description;
}

[Serializable]
public class CharacterEffectJsonData
{
    public string type;
    public int amount;
    public string target;
    public int cardId;
}

[Serializable]
public class CharacterAddressablesJsonData
{
    public string artwork;
    public string effect;
    public string sound;
}

