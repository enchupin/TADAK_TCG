using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/// <summary>
/// Addressables 자동 설정 툴
/// Tools → TCG → Setup Addressables
/// </summary>
public class AddressablesAutoSetup
{
    [MenuItem("Tools/TCG/Setup Addressables")]
    public static void SetupAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Addressables settings not found!\n\n" +
                "Please initialize Addressables:\n" +
                "Window → Asset Management → Addressables → Groups\n" +
                "Then click 'Create Addressables Settings'", "OK");
            return;
        }
        
        int cardCount = 0;
        int collectionCount = 0;
        
        // CardData 등록
        string[] cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Data/Cards" });
        foreach (string guid in cardGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
            
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            
            // 주소 설정: "Cards/CARD_101010"
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            // 파일명에서 cardId 추출 (예: "101012_몸에 좋은 약" → "101012")
            string cardId = fileName.Split('_')[0];
            entry.address = $"Cards/CARD_{cardId}";
            
            // Labels 추가: 캐릭터별 필터링용
            string characterLabel = cardData.character.ToString();
            if (!entry.labels.Contains(characterLabel))
            {
                entry.labels.Add(characterLabel);
            }
            
            Debug.Log($"등록: {entry.address}, Label: {characterLabel}");
            
            cardCount++;
        }
        
        // CardCollection 등록
        string[] collectionGuids = AssetDatabase.FindAssets("t:CardCollection", new[] { "Assets/Data/Cards" });
        foreach (string guid in collectionGuids)
        {
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = "CardCollection";
            collectionCount++;
        }
        
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Success", 
            $"Addressables setup complete!\n" +
            $"Cards: {cardCount}\n" +
            $"Collections: {collectionCount}", "OK");
    }
    
    [MenuItem("Tools/TCG/Check Addressables Status")]
    public static void CheckAddressablesStatus()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Addressables", 
                "Addressables is installed but not initialized.\n\n" +
                "To initialize:\n" +
                "Window → Asset Management → Addressables → Groups\n" +
                "Then click 'Create Addressables Settings'", "OK");
            return;
        }
        
        int totalEntries = 0;
        foreach (var group in settings.groups)
        {
            totalEntries += group.entries.Count;
        }
        
        EditorUtility.DisplayDialog("Addressables Status", 
            $"✅ Addressables is installed and ready!\n\n" +
            $"Total entries: {totalEntries}", "OK");
    }
}
