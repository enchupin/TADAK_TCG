using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class ProfileSaveCodec
{
    public static PlayerProfileSave Decode(string json, string ownerSteamId)
    {
        try
        {
            JObject document = JObject.Parse(json);
            JToken version = document["profileVersion"];
            if (version == null || version.Type != JTokenType.Integer)
            {
                throw new InvalidDataException("프로필 버전 정보가 없습니다");
            }

            // 최신 버전 및 다른 계정의 파일은 백업으로 되돌리지 않고 그대로 보존
            if (version.Value<long>() > PlayerProfileSave.CurrentProfileVersion)
            {
                throw new InvalidOperationException("더 최신 게임 버전에서 저장한 프로필입니다. 게임을 업데이트하세요");
            }

            if (version.Value<long>() < 1 || document["characters"]?.Type != JTokenType.Array
                || document["playerId"]?.Type != JTokenType.String
                || string.IsNullOrWhiteSpace(document.Value<string>("playerId")))
            {
                throw new InvalidDataException("프로필 필수 데이터가 올바르지 않습니다");
            }

            PlayerProfileSave profile = document.ToObject<PlayerProfileSave>();
            if (profile.profileVersion == 1)
            {
                if (!string.IsNullOrEmpty(ownerSteamId))
                {
                    throw new InvalidOperationException("계정 정보가 없는 기존 저장은 로컬 개발 프로필에서만 이전할 수 있습니다");
                }
                profile.ownerSteamId = string.Empty;
            }
            else if (document["ownerSteamId"]?.Type != JTokenType.String
                || document["rankingBestDamage"]?.Type != JTokenType.Integer)
            {
                throw new InvalidDataException("프로필 계정 또는 최고 기록 정보가 없습니다");
            }

            Validate(profile, ownerSteamId);
            return profile;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("프로필 JSON 형식이 올바르지 않습니다", exception);
        }
    }

    public static void Validate(PlayerProfileSave profile, string ownerSteamId)
    {
        if (profile.profileVersion > PlayerProfileSave.CurrentProfileVersion)
        {
            throw new InvalidOperationException("더 최신 게임 버전에서 저장한 프로필은 덮어쓸 수 없습니다");
        }
        if (!string.Equals(profile.ownerSteamId, ownerSteamId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("현재 Steam 계정과 프로필의 소유자가 다릅니다");
        }
        if (profile.bossBestDamage < 0 || profile.characters == null)
        {
            throw new InvalidDataException("프로필 기록 또는 캐릭터 목록이 올바르지 않습니다");
        }
        foreach (CharacterDeckListSave library in profile.characters)
        {
            if (library?.decks == null)
            {
                throw new InvalidDataException("캐릭터 덱 목록이 올바르지 않습니다");
            }
            foreach (CharacterDeckSave deck in library.decks)
            {
                if (deck?.cardIds == null || deck.cardIds.Count != 7)
                {
                    throw new InvalidDataException("저장 덱의 카드는 정확히 7장이어야 합니다");
                }
            }
        }
    }
}
