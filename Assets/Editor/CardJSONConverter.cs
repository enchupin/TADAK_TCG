using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// JSON card data to ScriptableObject converter.
/// </summary>
public class CardJSONConverter : EditorWindow
{
    private const string DefaultJsonFolderPath = "Assets/Resources/JsonData";
    private const string DefaultOutputPath = "Assets/Data/Cards";
    private const string CardCollectionPath = "Assets/Resources/CardCollection.asset";

    private string jsonFolderPath = DefaultJsonFolderPath;
    private string outputPath = DefaultOutputPath;

    [MenuItem("Tools/TCG/Card JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<CardJSONConverter>("Card Converter");
    }

    private void OnGUI()
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
            "Converts all '*Cards.json' files in the selected folder into CardData assets and updates CardCollection.",
            MessageType.Info);
    }

    private void ConvertJSONToScriptableObjects()
    {
        if (!Directory.Exists(jsonFolderPath))
        {
            Debug.LogError($"[CardJSONConverter] Folder not found: {jsonFolderPath}");
            EditorUtility.DisplayDialog("Error", "JSON folder path is invalid.", "OK");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*Cards.json");
        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"No '*Cards.json' found under {jsonFolderPath}", "OK");
            return;
        }

        EnsureDirectory(outputPath);

        CardCollection collection = GetOrCreateCardCollection();
        Dictionary<int, CardData> existingById = BuildCardIndexById();
        Dictionary<int, CardData> updatedById = new Dictionary<int, CardData>();

        int converted = 0;
        foreach (string filePath in jsonFiles)
        {
            converted += ConvertFileInternal(filePath, existingById, updatedById);
        }

        List<CardData> normalized = new List<CardData>(updatedById.Values);
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

    private int ConvertFileInternal(string path, Dictionary<int, CardData> existingById, Dictionary<int, CardData> updatedById)
    {
        string assetPath = path.Replace("\\", "/");
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"[CardJSONConverter] Could not load JSON asset: {assetPath}");
            return 0;
        }

        JObject root;
        try
        {
            root = JObject.Parse(jsonFile.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardJSONConverter] Invalid JSON in {assetPath}: {ex.Message}");
            return 0;
        }

        if (!(root["cards"] is JArray cardsArray))
        {
            Debug.LogWarning($"[CardJSONConverter] Missing 'cards' array in {assetPath}");
            return 0;
        }

        int count = 0;
        foreach (JToken token in cardsArray)
        {
            if (!(token is JObject cardObject))
            {
                continue;
            }

            int cardId = ReadInt(cardObject, "cardId");
            if (cardId <= 0)
            {
                Debug.LogWarning($"[CardJSONConverter] Skipped card without valid cardId in {assetPath}");
                continue;
            }

            CardData cardData = GetOrCreateCardData(cardObject, cardId, existingById);
            if (cardData == null)
            {
                continue;
            }

            UpdateCardData(cardData, cardObject);

            if (!AssetDatabase.Contains(cardData))
            {
                string newAssetPath = BuildCardAssetPath(cardData);
                AssetDatabase.CreateAsset(cardData, newAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(cardData);
            }

            existingById[cardId] = cardData;
            updatedById[cardId] = cardData;
            count++;
        }

        Debug.Log($"[CardJSONConverter] {Path.GetFileName(path)} converted: {count}");
        return count;
    }

    private CardData GetOrCreateCardData(JObject cardObject, int cardId, Dictionary<int, CardData> existingById)
    {
        if (existingById.TryGetValue(cardId, out CardData existing) && existing != null)
        {
            return existing;
        }

        string candidatePath = BuildCardAssetPath(cardId, ReadString(cardObject, "name"));
        CardData loaded = AssetDatabase.LoadAssetAtPath<CardData>(candidatePath);
        if (loaded != null)
        {
            return loaded;
        }

        return ScriptableObject.CreateInstance<CardData>();
    }

    private void UpdateCardData(CardData cardData, JObject cardObject)
    {
        cardData.cardId = ReadInt(cardObject, "cardId");
        cardData.cardName = ReadString(cardObject, "name");
        cardData.character = CharacterManager.GetCharacterEnumById(ReadInt(cardObject, "characterId"));
        cardData.cost = ReadInt(cardObject, "cost");
        cardData.description = ReadString(cardObject, "description");

        cardData.enforceCardIds.Clear();
        if (cardObject["enforce"] is JArray enforceArray)
        {
            foreach (JToken token in enforceArray)
            {
                int value = ReadInt(token);
                if (value != 0)
                {
                    cardData.enforceCardIds.Add(value);
                }
            }
        }

        cardData.artworkAddress = ReadString(cardObject.SelectToken("addressables.artwork"));
        cardData.effectAddress = ReadString(cardObject.SelectToken("addressables.effect"));
        cardData.soundAddress = ReadString(cardObject.SelectToken("addressables.sound"));

        cardData.effects.Clear();
        if (cardObject["effects"] is JArray effectsArray)
        {
            foreach (JToken token in effectsArray)
            {
                if (!(token is JObject effectObject))
                {
                    continue;
                }

                CardEffectData effect = ReadEffect(effectObject);
                if (effect != null)
                {
                    cardData.effects.Add(effect);
                }
            }
        }
    }

    private static CardEffectData ReadEffect(JObject effectObject)
    {
        if (effectObject == null)
        {
            return null;
        }

        CardEffectData effect = new CardEffectData
        {
            type = ParseEffectType(ReadString(effectObject, "type")),
            target = ParseTargetType(ReadString(effectObject, "target")),
            amount = ReadInt(effectObject, "amount"),
            amountFormula = ReadFirstNonEmptyString(effectObject, "amountFormula", "getAmount", "GetAmount", "getamount"),
            count = ReadInt(effectObject, "count"),
            baseDamage = ReadInt(effectObject, "baseDamage"),
            bonusPerCard = ReadInt(effectObject, "bonusPerCard"),
            hpThreshold = ReadFloat(effectObject, "hpThreshold"),
            multiplier = ReadFloat(effectObject, "multiplier"),
            stat = ReadString(effectObject, "stat"),
            buffId = ReadInt(effectObject, "buffId"),
            duration = ReadInt(effectObject, "duration"),
            keyword = ReadString(effectObject, "keyword")
        };

        JToken rawAmount = effectObject["amount"];
        if (string.IsNullOrWhiteSpace(effect.amountFormula) && rawAmount != null && rawAmount.Type == JTokenType.String)
        {
            effect.amountFormula = rawAmount.Value<string>();
        }

        if (effectObject["RandomCard"] is JArray randomCardArray)
        {
            effect.RandomCard = new List<RandomCardData>();
            foreach (JToken token in randomCardArray)
            {
                if (!(token is JObject randomCardObject))
                {
                    continue;
                }

                effect.RandomCard.Add(new RandomCardData
                {
                    cardId = ReadInt(randomCardObject, "cardId"),
                    weight = ReadInt(randomCardObject, "weight")
                });
            }
        }

        effect.cardIdList = ReadCardIdList(effectObject["cardId"]);

        if (effectObject["effect"] is JObject nestedEffectObject)
        {
            effect.nestedEffect = ReadEffect(nestedEffectObject);
        }

        if (effectObject["effects"] is JArray subEffectsArray)
        {
            effect.subEffects = new List<CardEffectData>();
            foreach (JToken token in subEffectsArray)
            {
                if (!(token is JObject subEffectObject))
                {
                    continue;
                }

                CardEffectData subEffect = ReadEffect(subEffectObject);
                if (subEffect != null)
                {
                    effect.subEffects.Add(subEffect);
                }
            }
        }

        if (effectObject["condition"] is JObject conditionObject)
        {
            effect.conditionData = ReadCondition(conditionObject);
        }

        if (effectObject["onAction"] is JObject onActionObject)
        {
            effect.onAction = ReadEffect(onActionObject);
        }

        return effect;
    }

    private static ConditionData ReadCondition(JObject conditionObject)
    {
        if (conditionObject == null)
        {
            return null;
        }

        ConditionData condition = new ConditionData
        {
            mode = ReadString(conditionObject, "mode", "And")
        };

        if (conditionObject["checks"] is JArray checksArray)
        {
            foreach (JToken token in checksArray)
            {
                if (!(token is JObject checkObject))
                {
                    continue;
                }

                condition.checks.Add(new CheckData
                {
                    subject = ReadString(checkObject, "subject"),
                    property = ReadString(checkObject, "property"),
                    param = ReadString(checkObject, "param"),
                    @operator = ReadString(checkObject, "operator"),
                    value = ReadString(checkObject, "value")
                });
            }
        }

        if (conditionObject["successEffect"] is JObject successObject)
        {
            condition.successEffect = ReadEffect(successObject);
        }
        else if (conditionObject["effects"] is JArray successEffectsArray)
        {
            condition.successEffect = ReadFirstEffect(successEffectsArray);
        }

        if (conditionObject["failEffect"] is JObject failObject)
        {
            condition.failEffect = ReadEffect(failObject);
        }

        return condition;
    }

    private static CardEffectData ReadFirstEffect(JArray effectsArray)
    {
        foreach (JToken token in effectsArray)
        {
            if (token is JObject effectObject)
            {
                return ReadEffect(effectObject);
            }
        }

        return null;
    }

    private static List<int> ReadCardIdList(JToken token)
    {
        List<int> result = new List<int>();
        if (token == null)
        {
            return result;
        }

        if (token is JArray array)
        {
            foreach (JToken item in array)
            {
                int value = ReadInt(item);
                if (value != 0)
                {
                    result.Add(value);
                }
            }

            return result;
        }

        int single = ReadInt(token);
        if (single != 0)
        {
            result.Add(single);
        }

        return result;
    }

    private string BuildCardAssetPath(CardData cardData)
    {
        return BuildCardAssetPath(cardData.cardId, cardData.cardName);
    }

    private string BuildCardAssetPath(int cardId, string cardName)
    {
        string rawName = $"{cardId}_{cardName}.asset";
        string safeName = SanitizeFileName(rawName);
        return Path.Combine(outputPath, safeName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid.ToString(), "_");
        }

        return fileName;
    }

    private Dictionary<int, CardData> BuildCardIndexById()
    {
        Dictionary<int, CardData> map = new Dictionary<int, CardData>();
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { outputPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (cardData == null || cardData.cardId == 0)
            {
                continue;
            }

            map[cardData.cardId] = cardData;
        }

        return map;
    }

    private static CardCollection GetOrCreateCardCollection()
    {
        EnsureDirectory("Assets/Resources");

        CardCollection collection = AssetDatabase.LoadAssetAtPath<CardCollection>(CardCollectionPath);
        if (collection != null)
        {
            return collection;
        }

        collection = ScriptableObject.CreateInstance<CardCollection>();
        AssetDatabase.CreateAsset(collection, CardCollectionPath);
        return collection;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static EffectType ParseEffectType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return EffectType.Damage;
        }

        switch (type)
        {
            case "Attack": return EffectType.Attack;
            case "Damage": return EffectType.Damage;
            case "Defense":
            case "Barrier": return EffectType.Barrier;
            case "Draw": return EffectType.Draw;
            case "Buff": return EffectType.Buff;
            case "Energy": return EffectType.Energy;
            case "Heal": return EffectType.Heal;
            case "MultiplyDefense":
            case "MultimediaDefense": return EffectType.MultiplyDefense;
            case "ConsumeDefense": return EffectType.ConsumeDefense;
            case "GenerateCard": return EffectType.GenerateCard;
            case "Keyword": return EffectType.Keyword;
            case "DiscardHand": return EffectType.DiscardHand;
            case "ChoiceDiscard": return EffectType.ChoiceDiscard;
            case "Pickup": return EffectType.Pickup;
            case "ExhaustHand": return EffectType.ExhaustHand;
            case "Repeat": return EffectType.Repeat;
            case "Conditional": return EffectType.Conditional;
            default: return EffectType.Damage;
        }
    }

    private static TargetType ParseTargetType(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return TargetType.SingleEnemy;
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
            default: return TargetType.SingleEnemy;
        }
    }

    private static string ReadFirstNonEmptyString(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            string value = ReadString(obj, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string ReadString(JObject obj, string key, string defaultValue = "")
    {
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadString(obj[key], defaultValue);
    }

    private static string ReadString(JToken token, string defaultValue = "")
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

    private static int ReadInt(JObject obj, string key, int defaultValue = 0)
    {
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadInt(obj[key], defaultValue);
    }

    private static int ReadInt(JToken token, int defaultValue = 0)
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

    private static float ReadFloat(JObject obj, string key, float defaultValue = 0f)
    {
        if (obj == null)
        {
            return defaultValue;
        }

        return ReadFloat(obj[key], defaultValue);
    }

    private static float ReadFloat(JToken token, float defaultValue = 0f)
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

