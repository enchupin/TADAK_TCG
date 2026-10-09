using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

public static class Program
{
    private static int passed;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { passed++; return; }
        throw new Exception(typeof(T).Name + " 오류가 발생해야 합니다");
    }
    private static void Reset()
    {
        typeof(ProfileSaveManager).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
    public static void Main(string[] args)
    {
        string root = Path.Combine(args[0], "Cases", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Application.persistentDataPath = root;
        string legacy = "{\"profileVersion\":1,\"playerId\":\"legacy\",\"characters\":[]}";
        File.WriteAllText(Path.Combine(root, "profile.json"), legacy);
        PlayerPrefs.Values["RankingModeBestDamage"] = 123;
        PlayerProfileSave local = ProfileSaveManager.CurrentProfile;
        Check(local.profileVersion == 2 && local.playerId == "legacy" && local.legacyBestDamage == 123, "기존 프로필/점수 이전");
        Check(File.ReadAllText(Path.Combine(root, "profile.json")) == legacy, "기존 원본 보존");
        Check(ProfileSaveManager.ProfileFilePath.Contains(Path.Combine("Saves", "Local")), "개발 경로");
        local.legacyBestDamage = 321;
        ProfileSaveManager.Save();
        Reset();
        Check(ProfileSaveManager.CurrentProfile.legacyBestDamage == 321, "이전 점수로 되돌아가지 않음");
        SteamClient.State = SteamClientState.Ready;
        SteamClient.UserId = 76561198000000001;
        Throws<InvalidOperationException>(() => ProfileSaveManager.LoadOrCreate());
        Reset();
        PlayerProfileSave first = ProfileSaveManager.CurrentProfile;
        Check(first.legacyBestDamage == 0 && first.playerId != "legacy", "개발 데이터 계정 자동 귀속 방지");
        first.legacyBestDamage = 999;
        ProfileSaveManager.Save(first);
        string firstPath = ProfileSaveManager.ProfileFilePath;
        Check(File.Exists(firstPath + ".bak") && !File.Exists(firstPath + ".tmp"), "원자 교체와 백업");
        Reset();
        SteamClient.UserId++;
        Check(ProfileSaveManager.CurrentProfile.legacyBestDamage == 0, "다른 계정 격리");
        Throws<InvalidOperationException>(() => ProfileSaveManager.Save(first));
        Reset();
        SteamClient.UserId--;
        Check(ProfileSaveManager.CurrentProfile.legacyBestDamage == 999, "계정 복귀");
        SteamClient.State = SteamClientState.Failed;
        Throws<InvalidOperationException>(() => ProfileSaveManager.LoadOrCreate());

        Reset();
        Application.persistentDataPath = Path.Combine(root, "OfflineBuild");
        PlayerProfileSave offline = ProfileSaveManager.CurrentProfile;
        Check(offline.ownerSteamId == string.Empty && ProfileSaveManager.ProfileFilePath.Contains(Path.Combine("Saves", "Local")),
            "Steam 없는 빌드는 별도 로컬 저장 사용");
        Check(offline.characters.Count == 10 && offline.characters.TrueForAll(library => library.decks.Count == 1
            && library.decks[0].cardIds.Count == 7), "Steam 초기화 실패 시 모든 캐릭터 기본 덱 생성");
        offline.FindLibrary((int)Character.Mio).decks[0].name = "오프라인 덱";
        ProfileSaveManager.Save(offline);
        Reset();
        Check(ProfileSaveManager.CurrentProfile.FindLibrary((int)Character.Mio).decks[0].name == "오프라인 덱",
            "Steam 없는 빌드 재실행 후 덱 유지");
        SteamClient.State = SteamClientState.Ready;
        Check(ProfileSaveManager.CurrentProfile.ownerSteamId == string.Empty, "실행 중 Steam 복구에도 로컬 저장 유지");
        ProfileSaveManager.Save();
        Reset();
        Check(ProfileSaveManager.CurrentProfile.ownerSteamId == SteamClient.UserId.ToString()
            && ProfileSaveManager.CurrentProfile.FindLibrary((int)Character.Mio).decks[0].name != "오프라인 덱",
            "재시작 후 Steam 계정 저장은 로컬과 분리");
        Reset();
        Application.persistentDataPath = root;
        Check(ProfileSaveManager.CurrentProfile.legacyBestDamage == 999, "기존 Steam 계정 기록 보존");

        string path = Path.Combine(root, "Recovery", "profile.json");
        string valid = JsonConvert.SerializeObject(first);
        Check(valid.Contains("\"rankingBestDamage\":999"), "이름 변경 후에도 기존 최고 기록 저장 키 유지");
        Check(ProfileSaveCodec.Decode(valid, first.ownerSteamId).legacyBestDamage == 999, "기존 저장 키에서 보스모드 최고 기록 복원");
        Func<string, PlayerProfileSave> decode = json => ProfileSaveCodec.Decode(json, first.ownerSteamId);
        ProfileFileStore.Write(path, valid);
        ProfileFileStore.Write(path, valid);
        File.WriteAllText(path, "{truncated");
        Check(ProfileFileStore.Load(path, decode).legacyBestDamage == 999, "손상 본문 백업 복구");
        Check(Directory.GetFiles(Path.GetDirectoryName(path), "*.corrupt.*").Length == 1, "손상 원본 보존");
        Check(decode(File.ReadAllText(path)).legacyBestDamage == 999, "복구 파일 설치");
        File.Delete(path);
        File.Delete(path + ".bak");
        File.WriteAllText(path + ".tmp", valid);
        Check(ProfileFileStore.Load(path, decode).legacyBestDamage == 999 && File.Exists(path), "첫 저장 중 종료 복구");
        File.Delete(path + ".tmp");
        File.WriteAllText(path + ".bak", valid);
        string future = valid.Replace("\"profileVersion\":2", "\"profileVersion\":999");
        File.WriteAllText(path, future);
        Throws<InvalidOperationException>(() => ProfileFileStore.Load(path, decode));
        Check(File.ReadAllText(path) == future, "미래 버전 원본 보존");
        File.WriteAllText(path, valid.Replace(first.ownerSteamId, "76561198000000099"));
        Throws<InvalidOperationException>(() => ProfileFileStore.Load(path, decode));
        File.WriteAllText(path, "{}");
        File.WriteAllText(path + ".bak", "{}");
        Throws<InvalidDataException>(() => ProfileFileStore.Load(path, decode));
        Check(File.ReadAllText(path) == "{}", "전부 손상되어도 덮어쓰기 방지");
        Throws<InvalidOperationException>(() => ProfileSaveCodec.Decode(legacy, first.ownerSteamId));
        first.characters[0].decks.Clear();
        first.characters[0].decks.Add(CharacterDeckSave.Create("검증 덱", new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6, 7 }));
        ProfileSaveCodec.Decode(JsonConvert.SerializeObject(first), first.ownerSteamId);
        passed++;
        first.characters[0].decks[0].cardIds.Add(8);
        Check(ProfileSaveCodec.Decode(JsonConvert.SerializeObject(first), first.ownerSteamId)
            .characters[0].decks[0].cardIds.Count == 8, "8장 저장덱 불러오기");
        first.characters[0].decks[0].cardIds.Add(9);
        Throws<InvalidDataException>(() => ProfileSaveCodec.Decode(JsonConvert.SerializeObject(first), first.ownerSteamId));
        first.characters[0].decks[0].cardIds = new System.Collections.Generic.List<int> { 1 };
        Check(ProfileSaveCodec.Decode(JsonConvert.SerializeObject(first), first.ownerSteamId)
            .characters[0].decks[0].cardIds.Count == 1, "최대 장수 이하 저장덱 허용");
        first.characters[0].decks[0].cardIds.Clear();
        Throws<InvalidDataException>(() => ProfileSaveCodec.Decode(JsonConvert.SerializeObject(first), first.ownerSteamId));
        first.characters[0].decks[0].cardIds = null;
        Throws<InvalidDataException>(() => ProfileSaveCodec.Decode(JsonConvert.SerializeObject(first), first.ownerSteamId));

        Reset();
        SteamClient.State = SteamClientState.Ready;
        SteamClient.UserId += 10;
        PlayerProfileSave renameProfile = ProfileSaveManager.CurrentProfile;
        CharacterDeckListSave mio = renameProfile.FindLibrary((int)Character.Mio);
        Check(mio.decks.Count == 1 && mio.decks[0].cardIds.Count == 7
            && mio.decks[0].cardIds[0] == 204010 && mio.decks[0].cardIds[6] == 204070, "미오 기본 덱 자동 생성");
        Check(mio.selectedDeckId == mio.decks[0].deckId, "생성된 기본 덱 선택");

        string renamePath = ProfileSaveManager.ProfileFilePath;
        ProfileSaveManager.Save(renameProfile);
        mio.decks[0].name = "재실행 후 유지할 이름";
        renameProfile.legacyBestDamage = 456;
        string unityJson = ProfileSaveCodec.Encode(renameProfile).Replace("\"rankingBestDamage\"", "\"bossBestDamage\"");
        File.WriteAllText(renamePath, unityJson);
        Reset();
        renameProfile = ProfileSaveManager.CurrentProfile;
        mio = renameProfile.FindLibrary((int)Character.Mio);
        Check(mio.decks[0].name == "재실행 후 유지할 이름" && renameProfile.legacyBestDamage == 456,
            "이전 Unity 저장 형식도 최신 이름과 최고 기록 유지");
        Check(Directory.GetFiles(Path.GetDirectoryName(renamePath), "*.corrupt.*").Length == 0,
            "호환 가능한 최신 저장을 손상 파일로 오인하지 않음");

        mio.decks[0].name = "수정된 이름";
        string retainedDeckId = mio.decks[0].deckId;
        ProfileSaveManager.Save(renameProfile);
        Check(File.ReadAllText(renamePath).Contains("\"rankingBestDamage\"")
            && !File.ReadAllText(renamePath).Contains("\"bossBestDamage\""), "저장과 읽기의 최고 기록 키 통일");
        Reset();
        renameProfile = ProfileSaveManager.CurrentProfile;
        mio = renameProfile.FindLibrary((int)Character.Mio);
        Check(mio.decks[0].name == "수정된 이름" && mio.decks[0].deckId == retainedDeckId,
            "덱 이름 변경 저장 후 재실행 유지");

        CharacterDeckListSave isla = renameProfile.FindLibrary((int)Character.Isla);
        isla.decks[0].name = "기존 덱 유지";
        mio.decks.Clear();
        ProfileSaveManager.Save(renameProfile);
        Check(mio.decks.Count == 1 && mio.decks[0].deckId != retainedDeckId
            && mio.selectedDeckId == mio.decks[0].deckId, "마지막 저장덱 삭제 후 기본 덱 즉시 복원");
        Check(isla.decks[0].name == "기존 덱 유지", "다른 캐릭터의 저장덱 이름 보존");
        Reset();
        renameProfile = ProfileSaveManager.CurrentProfile;
        mio = renameProfile.FindLibrary((int)Character.Mio);
        Check(mio.decks.Count == 1 && mio.decks[0].cardIds.Count == 7, "복원한 기본 덱 재실행 유지");

        mio.decks.Clear();
        File.WriteAllText(renamePath, ProfileSaveCodec.Encode(renameProfile));
        Reset();
        renameProfile = ProfileSaveManager.CurrentProfile;
        Check(renameProfile.FindLibrary((int)Character.Mio).decks.Count == 1, "기존 빈 저장덱 라이브러리 자동 복구");
        Check(ProfileSaveCodec.Decode(File.ReadAllText(renamePath), renameProfile.ownerSteamId)
            .FindLibrary((int)Character.Mio).decks.Count == 1, "복구 결과 디스크 저장");
        renameProfile.bossModeLastDamage = 3_000_000_000L;
        renameProfile.bossModeBestDamage = 5_000_000_000L;
        renameProfile.bossModeLastPlayedAtUtc = "2026-10-08T00:00:00.0000000Z";
        ProfileSaveManager.Save(renameProfile);
        Reset();
        PlayerProfileSave bossProfile = ProfileSaveManager.CurrentProfile;
        Check(bossProfile.bossModeLastDamage == 3_000_000_000L, "보스모드 최근 피해량 64비트 저장과 복원");
        Check(bossProfile.bossModeBestDamage == 5_000_000_000L, "보스모드 최고 피해량 저장과 복원");
        Check(DateTime.Parse(bossProfile.bossModeLastPlayedAtUtc).ToUniversalTime()
            == new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), "보스모드 기록 시각 저장과 복원");
        Check(bossProfile.FindLibrary((int)Character.Mio).decks.Count == 1, "보스모드 기록 저장 후 덱 보존");
        bossProfile.bossModeLastDamage = -1;
        Throws<InvalidDataException>(() => ProfileSaveCodec.Validate(bossProfile, bossProfile.ownerSteamId));
        Console.WriteLine($"저장 시스템 검증 {passed}개 통과");
    }
}
