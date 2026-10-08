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
        Check(local.profileVersion == 2 && local.playerId == "legacy" && local.bossBestDamage == 123, "기존 프로필/점수 이전");
        Check(File.ReadAllText(Path.Combine(root, "profile.json")) == legacy, "기존 원본 보존");
        Check(ProfileSaveManager.ProfileFilePath.Contains(Path.Combine("Saves", "Local")), "개발 경로");
        local.bossBestDamage = 321;
        ProfileSaveManager.Save();
        Reset();
        Check(ProfileSaveManager.CurrentProfile.bossBestDamage == 321, "이전 점수로 되돌아가지 않음");
        SteamClient.State = SteamClientState.Ready;
        SteamClient.UserId = 76561198000000001;
        Throws<InvalidOperationException>(() => ProfileSaveManager.LoadOrCreate());
        Reset();
        PlayerProfileSave first = ProfileSaveManager.CurrentProfile;
        Check(first.bossBestDamage == 0 && first.playerId != "legacy", "개발 데이터 계정 자동 귀속 방지");
        first.bossBestDamage = 999;
        ProfileSaveManager.Save(first);
        string firstPath = ProfileSaveManager.ProfileFilePath;
        Check(File.Exists(firstPath + ".bak") && !File.Exists(firstPath + ".tmp"), "원자 교체와 백업");
        Reset();
        SteamClient.UserId++;
        Check(ProfileSaveManager.CurrentProfile.bossBestDamage == 0, "다른 계정 격리");
        Throws<InvalidOperationException>(() => ProfileSaveManager.Save(first));
        Reset();
        SteamClient.UserId--;
        Check(ProfileSaveManager.CurrentProfile.bossBestDamage == 999, "계정 복귀");
        Reset();
        SteamClient.State = SteamClientState.Failed;
        Throws<InvalidOperationException>(() => ProfileSaveManager.LoadOrCreate());

        string path = Path.Combine(root, "Recovery", "profile.json");
        string valid = JsonConvert.SerializeObject(first);
        Check(valid.Contains("\"rankingBestDamage\":999"), "이름 변경 후에도 기존 최고 기록 저장 키 유지");
        Check(ProfileSaveCodec.Decode(valid, first.ownerSteamId).bossBestDamage == 999, "기존 저장 키에서 보스모드 최고 기록 복원");
        Func<string, PlayerProfileSave> decode = json => ProfileSaveCodec.Decode(json, first.ownerSteamId);
        ProfileFileStore.Write(path, valid);
        ProfileFileStore.Write(path, valid);
        File.WriteAllText(path, "{truncated");
        Check(ProfileFileStore.Load(path, decode).bossBestDamage == 999, "손상 본문 백업 복구");
        Check(Directory.GetFiles(Path.GetDirectoryName(path), "*.corrupt.*").Length == 1, "손상 원본 보존");
        Check(decode(File.ReadAllText(path)).bossBestDamage == 999, "복구 파일 설치");
        File.Delete(path);
        File.Delete(path + ".bak");
        File.WriteAllText(path + ".tmp", valid);
        Check(ProfileFileStore.Load(path, decode).bossBestDamage == 999 && File.Exists(path), "첫 저장 중 종료 복구");
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
        Console.WriteLine($"저장 시스템 검증 {passed}개 통과");
    }
}
