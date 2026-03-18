using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ProfileSaveManager
{
    private const string ProfileFileName = "profile.json";

    private static PlayerProfileSave currentProfile;

    public static string ProfileFilePath => Path.Combine(Application.persistentDataPath, ProfileFileName);

    public static PlayerProfileSave CurrentProfile => LoadOrCreate();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadOrCreate();
    }

    public static PlayerProfileSave LoadOrCreate()
    {
        if (currentProfile != null)
        {
            return currentProfile;
        }

        PlayerProfileSave loadedProfile = TryLoadFromDisk();
        bool shouldSave = false;

        if (loadedProfile == null)
        {
            loadedProfile = CreateDefaultProfile();
            shouldSave = true;
            Debug.Log($"[ProfileSaveManager] 새 프로필을 생성했습니다: {ProfileFilePath}");
        }

        if (RepairProfileData(loadedProfile))
        {
            shouldSave = true;
        }

        currentProfile = loadedProfile;

        if (shouldSave)
        {
            Save(currentProfile);
        }

        return currentProfile;
    }

    public static void Save()
    {
        Save(LoadOrCreate());
    }

    public static void Save(PlayerProfileSave profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("[ProfileSaveManager] 저장할 프로필이 없습니다");
            return;
        }

        RepairProfileData(profile);
        profile.TouchUpdatedAtUtc();

        string directoryPath = Path.GetDirectoryName(ProfileFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string json = JsonUtility.ToJson(profile, true);
        File.WriteAllText(ProfileFilePath, json);

        currentProfile = profile;

        Debug.Log($"[ProfileSaveManager] 프로필 저장 완료: {ProfileFilePath}");
    }

    public static CharacterDeckLibrarySave GetOrCreateLibrary(int characterId)
    {
        PlayerProfileSave profile = LoadOrCreate();
        CharacterDeckLibrarySave library = profile.FindLibrary(characterId);
        if (library != null)
        {
            return library;
        }

        CharacterData characterData = CharacterManager.GetCharacter(characterId);
        if (characterData == null)
        {
            Debug.LogWarning($"[ProfileSaveManager] 존재하지 않는 characterId로 라이브러리를 만들 수 없습니다: {characterId}");
            return null;
        }

        library = CreateLibrary(characterId);
        profile.characters.Add(library);
        Save(profile);
        return library;
    }

    public static CharacterDeckLibrarySave GetOrCreateLibrary(Character character)
    {
        return GetOrCreateLibrary(CharacterManager.GetIdByCharacterEnum(character));
    }

    private static PlayerProfileSave TryLoadFromDisk()
    {
        if (!File.Exists(ProfileFilePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(ProfileFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[ProfileSaveManager] profile.json이 비어 있어 새 프로필을 생성합니다");
                return null;
            }

            PlayerProfileSave profile = JsonUtility.FromJson<PlayerProfileSave>(json);
            if (profile == null)
            {
                Debug.LogWarning("[ProfileSaveManager] profile.json 역직렬화에 실패해 새 프로필을 생성합니다");
            }

            return profile;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ProfileSaveManager] profile.json 로드 중 예외가 발생했습니다: {ex.Message}");
            return null;
        }
    }

    private static PlayerProfileSave CreateDefaultProfile()
    {
        PlayerProfileSave profile = new PlayerProfileSave
        {
            profileVersion = PlayerProfileSave.CurrentProfileVersion,
            playerId = Guid.NewGuid().ToString("N")
        };

        profile.TouchUpdatedAtUtc();
        profile.characters = new List<CharacterDeckLibrarySave>();

        EnsureCharacterLibraries(profile);
        return profile;
    }

    private static bool RepairProfileData(PlayerProfileSave profile)
    {
        bool hasChanges = false;

        if (profile.profileVersion <= 0)
        {
            profile.profileVersion = PlayerProfileSave.CurrentProfileVersion;
            hasChanges = true;
        }

        if (string.IsNullOrWhiteSpace(profile.playerId))
        {
            profile.playerId = Guid.NewGuid().ToString("N");
            hasChanges = true;
        }

        if (profile.characters == null)
        {
            profile.characters = new List<CharacterDeckLibrarySave>();
            hasChanges = true;
        }

        if (string.IsNullOrWhiteSpace(profile.updatedAtUtc))
        {
            profile.TouchUpdatedAtUtc();
            hasChanges = true;
        }

        if (RepairLibraries(profile.characters))
        {
            hasChanges = true;
        }

        if (EnsureCharacterLibraries(profile))
        {
            hasChanges = true;
        }

        return hasChanges;
    }

    private static bool RepairLibraries(List<CharacterDeckLibrarySave> libraries)
    {
        bool hasChanges = false;
        HashSet<int> seenCharacterIds = new HashSet<int>();

        for (int i = libraries.Count - 1; i >= 0; i--)
        {
            CharacterDeckLibrarySave library = libraries[i];
            if (library == null)
            {
                libraries.RemoveAt(i);
                hasChanges = true;
                continue;
            }

            if (library.decks == null)
            {
                library.decks = new List<CharacterDeckSave>();
                hasChanges = true;
            }

            if (!seenCharacterIds.Add(library.characterId))
            {
                libraries.RemoveAt(i);
                hasChanges = true;
                continue;
            }

            if (RepairDecks(library))
            {
                hasChanges = true;
            }
        }

        return hasChanges;
    }

    private static bool RepairDecks(CharacterDeckLibrarySave library)
    {
        bool hasChanges = false;
        HashSet<string> seenDeckIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = library.decks.Count - 1; i >= 0; i--)
        {
            CharacterDeckSave deck = library.decks[i];
            if (deck == null)
            {
                library.decks.RemoveAt(i);
                hasChanges = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(deck.deckId))
            {
                deck.deckId = Guid.NewGuid().ToString("N");
                hasChanges = true;
            }

            if (!seenDeckIds.Add(deck.deckId))
            {
                library.decks.RemoveAt(i);
                hasChanges = true;
                continue;
            }

            if (deck.cardIds == null)
            {
                deck.cardIds = new List<int>();
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(deck.name))
            {
                deck.name = "이름 없는 덱";
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(deck.createdAtUtc))
            {
                deck.createdAtUtc = DateTime.UtcNow.ToString("o");
                hasChanges = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(library.selectedDeckId) && library.FindDeck(library.selectedDeckId) == null)
        {
            library.selectedDeckId = string.Empty;
            hasChanges = true;
        }

        return hasChanges;
    }

    private static bool EnsureCharacterLibraries(PlayerProfileSave profile)
    {
        bool hasChanges = false;
        List<CharacterData> allCharacters = CharacterManager.GetAllCharacters();
        if (allCharacters == null)
        {
            return false;
        }

        for (int i = 0; i < allCharacters.Count; i++)
        {
            CharacterData characterData = allCharacters[i];
            if (characterData == null)
            {
                continue;
            }

            if (profile.FindLibrary(characterData.characterId) != null)
            {
                continue;
            }

            profile.characters.Add(CreateLibrary(characterData.characterId));
            hasChanges = true;
        }

        return hasChanges;
    }

    private static CharacterDeckLibrarySave CreateLibrary(int characterId)
    {
        return new CharacterDeckLibrarySave
        {
            characterId = characterId,
            selectedDeckId = string.Empty,
            decks = new List<CharacterDeckSave>()
        };
    }
}
