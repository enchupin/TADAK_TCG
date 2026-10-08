// ReSharper disable CheckNamespace
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

public static class ProfileSaveManager
{
    private const string ProfileFileName = "profile.json";

    private static PlayerProfileSave currentProfile;


    // 현재 실행에서 사용 중인 저장 소유자
    private static string currentOwner;

    // 프로필 저장 파일 경로
    public static string ProfileFilePath
    {
        get
        {
            // 경로를 만들기 전에 저장 소유자를 확인
            string owner = ResolveOwner();

            // 경로를 반환
            return Path.Combine(
                Application.persistentDataPath,
                "Saves",
                string.IsNullOrEmpty(owner) ? "Local" : owner,
                ProfileFileName);
        }
    }

    public static PlayerProfileSave CurrentProfile => LoadOrCreate();

    // ResetStatics() 호출, 호출 시점은 첫 씬이 로드되기 직전
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]

    // 게임이 시작될 때 정적 변수 프로필 캐시와 계정 정보를 초기화
    private static void ResetStatics()
    {
        currentProfile = null;
        currentOwner = null;
    }

    // 현재 저장 소유자를 결정하고, 실행 중 계정 변경 검사
    private static string ResolveOwner()
    {
        // StreamClient 초기화가 저장 시스템보다 늦게 실행되는 문제를 방지하기 위해 초기화 실행
        SteamClient.EnsureInitialized();
        string owner;
        if (SteamClient.TryGetUser(out ulong userId, out _))
        {
            owner = userId.ToString(CultureInfo.InvariantCulture);
        }
        else if (SteamClient.State == SteamClientState.Disabled)
        {
            owner = string.Empty;
        }
        else
        {
            throw new InvalidOperationException("Steam 계정을 확인할 수 없어 프로필 접근을 중단합니다. Steam 실행 상태와 앱 권한을 확인하세요");
        }

        if (currentOwner != null && currentOwner != owner)
        {
            throw new InvalidOperationException("실행 중 Steam 계정이 변경되었습니다. 게임을 다시 시작하세요");
        }
        currentOwner = owner;
        return owner;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadOrCreate();
    }

    public static PlayerProfileSave LoadOrCreate()
    {
        ResolveOwner();
        if (currentProfile != null)
        {
            return currentProfile;
        }

        PlayerProfileSave loadedProfile = TryLoadFromDisk();
        bool shouldSave = false;

        if (loadedProfile == null && string.IsNullOrEmpty(currentOwner))
        {
            string legacyPath = Path.Combine(Application.persistentDataPath, ProfileFileName);
            loadedProfile = ProfileFileStore.Load(legacyPath, DecodeProfile);
            shouldSave = loadedProfile != null;
        }

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

        if (shouldSave)
        {
            Save(loadedProfile);
        }

        currentProfile = loadedProfile;

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

        string owner = ResolveOwner();
        ProfileSaveCodec.Validate(profile, owner);
        RepairProfileData(profile);
        profile.TouchUpdatedAtUtc();

        string directoryPath = Path.GetDirectoryName(ProfileFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string json = ProfileSaveCodec.Encode(profile);
        ProfileFileStore.Write(ProfileFilePath, json);

        currentProfile = profile;

        Debug.Log($"[ProfileSaveManager] 프로필 저장 완료: {ProfileFilePath}");
    }

    public static CharacterDeckListSave GetOrCreateLibrary(int characterId)
    {
        PlayerProfileSave profile = LoadOrCreate();
        CharacterDeckListSave library = profile.FindLibrary(characterId);
        if (library != null)
        {
            return library;
        }

        if (!IsSupportedCharacterId(characterId))
        {
            Debug.LogWarning($"[ProfileSaveManager] 존재하지 않는 characterId로 라이브러리를 만들 수 없습니다: {characterId}");
            return null;
        }

        library = CreateLibrary(characterId);
        profile.characters.Add(library);
        Save(profile);
        return library;
    }

    public static CharacterDeckListSave GetOrCreateLibrary(Character character)
    {
        return GetOrCreateLibrary(CharacterManager.GetIdByCharacterEnum(character));
    }

    private static PlayerProfileSave TryLoadFromDisk()
    {
        try
        {
            return ProfileFileStore.Load(ProfileFilePath, DecodeProfile);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ProfileSaveManager] profile.json 로드 중 예외가 발생했습니다: {ex.Message}");
            throw;
        }
    }

    private static PlayerProfileSave DecodeProfile(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[ProfileSaveManager] profile.json이 비어 있어 새 프로필을 생성합니다");
            Debug.LogError("[ProfileSaveManager] 데이터 보호를 위해 새 프로필 생성 대신 복구를 시도합니다");
            throw new InvalidDataException("프로필 파일이 비어 있습니다");
        }

        if (json.Trim() == "null")
        {
            Debug.LogWarning("[ProfileSaveManager] profile.json 역직렬화에 실패해 새 프로필을 생성합니다");
            Debug.LogError("[ProfileSaveManager] 데이터 보호를 위해 새 프로필 생성 대신 복구를 시도합니다");
            throw new InvalidDataException("프로필을 역직렬화할 수 없습니다");
        }

        return ProfileSaveCodec.Decode(json, currentOwner);
    }

    private static PlayerProfileSave CreateDefaultProfile()
    {
        PlayerProfileSave profile = new PlayerProfileSave
        {
            profileVersion = PlayerProfileSave.CurrentProfileVersion,
            playerId = Guid.NewGuid().ToString("N"),
            ownerSteamId = currentOwner,
            legacyBestDamage = string.IsNullOrEmpty(currentOwner)
                ? Math.Max(0, PlayerPrefs.GetInt("RankingModeBestDamage", 0)) : 0
        };

        profile.TouchUpdatedAtUtc();
        profile.characters = new List<CharacterDeckListSave>();

        EnsureCharacterLibraries(profile);
        return profile;
    }

    private static bool RepairProfileData(PlayerProfileSave profile)
    {
        bool hasChanges = false;

        if (profile.profileVersion < PlayerProfileSave.CurrentProfileVersion)
        {
            profile.legacyBestDamage = Math.Max(profile.legacyBestDamage,
                Math.Max(0, PlayerPrefs.GetInt("RankingModeBestDamage", 0)));
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
            profile.characters = new List<CharacterDeckListSave>();
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

    private static bool RepairLibraries(List<CharacterDeckListSave> libraries)
    {
        bool hasChanges = false;
        HashSet<int> seenCharacterIds = new HashSet<int>();

        for (int i = libraries.Count - 1; i >= 0; i--)
        {
            CharacterDeckListSave library = libraries[i];
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

            if (!IsSupportedCharacterId(library.characterId))
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

    private static bool RepairDecks(CharacterDeckListSave library)
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
        Array allCharacters = Enum.GetValues(typeof(Character));
        for (int i = 0; i < allCharacters.Length; i++)
        {
            int characterId = (int)allCharacters.GetValue(i);
            if (!IsSupportedCharacterId(characterId))
            {
                continue;
            }

            CharacterDeckListSave library = profile.FindLibrary(characterId);
            if (library == null)
            {
                library = CreateLibrary(characterId);
                profile.characters.Add(library);
                hasChanges = true;
            }

            if (EnsureStarterDeck(library))
            {
                hasChanges = true;
            }
        }

        return hasChanges;
    }

    private static CharacterDeckListSave CreateLibrary(int characterId)
    {
        CharacterDeckListSave library = new CharacterDeckListSave
        {
            characterId = characterId,
            selectedDeckId = string.Empty,
            decks = new List<CharacterDeckSave>()
        };
        EnsureStarterDeck(library);
        return library;
    }

    private static bool EnsureStarterDeck(CharacterDeckListSave library)
    {
        if (library.decks.Count > 0)
        {
            return false;
        }

        List<int> starterCardIds = CharacterManager.GetStarterCardIds(library.characterId);
        if (!CharacterDeckSave.IsValidCardCount(starterCardIds.Count))
        {
            throw new InvalidOperationException($"기본 덱 카드 구성이 올바르지 않습니다: {library.characterId}");
        }

        CharacterDeckSave starterDeck = CharacterDeckSave.Create("기본 덱", starterCardIds);
        library.decks.Add(starterDeck);
        library.selectedDeckId = starterDeck.deckId;
        return true;
    }

    private static bool IsSupportedCharacterId(int characterId)
    {
        if (!Enum.IsDefined(typeof(Character), characterId))
        {
            return false;
        }

        return characterId != (int)Character.Monster;
    }
}
