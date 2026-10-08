// ReSharper disable CheckNamespace
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfileSave
{
    // 데이터 구조 버전 2
    public const int CurrentProfileVersion = 2;
    public int profileVersion = CurrentProfileVersion;

    // 플레이어 아이디
    public string playerId = string.Empty;

    // 저장 파일이 어느 Steam 계정에 속하는지 기록
    public string ownerSteamId = string.Empty;

    // 랭킹모드 최고 피해량 기록
    // 기존 저장 파일과 호환되도록 JSON 키는 유지
    [Newtonsoft.Json.JsonProperty("rankingBestDamage")]
    public int legacyBestDamage;

    public long bossModeLastDamage;
    public long bossModeBestDamage;
    public string bossModeLastPlayedAtUtc = string.Empty;

    // 현재 시각
    public string updatedAtUtc = string.Empty;

    // 플레이어가 소유중인 직업별 저장 덱
    public List<CharacterDeckListSave> characters = new();

    // 특정 직업의 덱 리스트를 반환
    public CharacterDeckListSave FindLibrary(int characterId)
    {
        if (characters == null)
        {
            return null;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDeckListSave library = characters[i];
            if (library == null)
            {
                continue;
            }
            if (library.characterId == characterId)
            {
                return library;
            } 
        }

        return null;
    }


    // 현재 시각을 updatedAtUtc에 기록
    public void TouchUpdatedAtUtc()
    {
        updatedAtUtc = DateTime.UtcNow.ToString("o");
    }
}
