using System.Collections.Generic;
using System.Globalization;
using static BattleRuntimeDefinitions;

/// <summary>
/// 게임 데이터(카드 · 버프 · 키워드 · 캐릭터)와 UI 문구의 현재 언어 조회
/// </summary>
// 키 규칙: card.<id>.name/.desc, buff.<id>.name/.desc, keyword.<id>.name/.desc, character.<id>.name/.identity
// 직접 작성하는 문구는 ui. · intent. 접두어 (card. 등 생성 표 접두어와 겹치지 않게)
public static class GameText
{
    // 동적 이름 인자: 동력 · 우피: 보호 · 우피: 공격은 중첩을 공유하며 우피 모드에 따라 활성 버프가 바뀜
    public const string RuneBuffArgument = "runeBuff";

    public static string CardName(int cardId, string fallback = "")
    {
        return Lookup("card." + ToKeyId(cardId) + ".name", fallback);
    }

    // 참조 토큰 · 자리표시가 남아 있는 원문 틀 (표시 전 CardDescriptionFormatter에서 해석)
    public static string CardDescription(int cardId, string fallback = "")
    {
        return Lookup("card." + ToKeyId(cardId) + ".desc", fallback);
    }

    public static string BuffName(int buffId, string fallback = "")
    {
        return Lookup("buff." + ToKeyId(buffId) + ".name", fallback);
    }

    public static string BuffDescription(int buffId, string fallback = "")
    {
        return Lookup("buff." + ToKeyId(buffId) + ".desc", fallback);
    }

    public static string KeywordName(int keywordId, string fallback = "")
    {
        return Lookup("keyword." + ToKeyId(keywordId) + ".name", fallback);
    }

    public static string KeywordDescription(int keywordId, string fallback = "")
    {
        return Lookup("keyword." + ToKeyId(keywordId) + ".desc", fallback);
    }

    public static string CharacterName(int characterId, string fallback = "")
    {
        return Lookup("character." + ToKeyId(characterId) + ".name", fallback);
    }

    public static string IdentityDescription(int characterId, string fallback = "")
    {
        return Lookup("character." + ToKeyId(characterId) + ".identity", fallback);
    }

    // UI 문구: 인자 · 복수형 · 참조를 현재 언어로 해석
    public static string Ui(string key, IReadOnlyDictionary<string, object> args = null)
    {
        return LocalizationManager.GetText(key, WithDefaultArguments(args), ResolvePlainReference);
    }

    // 표에서 꺼낸 틀을 참조 이름 그대로(괄호 없이) 해석
    public static string FormatPlain(string template, IReadOnlyDictionary<string, object> args = null)
    {
        return LocalizationManager.Format(template, WithDefaultArguments(args), ResolvePlainReference);
    }

    // [buff:id] → 현재 언어 이름, 표시 문구가 있으면 그 문구 (격변화형)
    public static bool ResolvePlainReference(LocalizedReference reference, out string text)
    {
        if (reference.HasDisplayOverride)
        {
            text = reference.DisplayOverride;
            return true;
        }

        text = ResolveReferenceName(reference);
        return !string.IsNullOrEmpty(text);
    }

    // 카드 툴팁 등 기존 (이름) 표기를 유지하는 곳
    public static bool ResolveParenthesizedReference(LocalizedReference reference, out string text)
    {
        if (!ResolvePlainReference(reference, out string name))
        {
            text = null;
            return false;
        }

        text = "(" + name + ")";
        return true;
    }

    public static string ResolveReferenceName(LocalizedReference reference)
    {
        return reference.Kind switch
        {
            LocalizedReference.Buff => BuffMetadataDatabase.TryGetBuffData(reference.Id, out BuffData data) && data != null
                ? data.name
                : BuffName(reference.Id),
            LocalizedReference.Keyword => KeywordDatabase.GetKeywordName(reference.Id),
            LocalizedReference.Card => CardName(reference.Id),
            LocalizedReference.Character => CharacterManager.GetCharacter(reference.Id)?.characterName ?? CharacterName(reference.Id),
            _ => null
        };
    }

    // 우피 모드에 따른 동력 계열 활성 버프
    public static int GetCurrentRuneBuffId()
    {
        PlayerData player = PlayerData.Instance;
        if (player != null && player.wuppiMode == WuppiModeState.Guard)
        {
            return WuppiGuardBuffId;
        }

        return player != null && player.wuppiMode == WuppiModeState.Attack ? WuppiAttackBuffId : RuneBuffId;
    }

    public static string GetCurrentRuneBuffName()
    {
        int buffId = GetCurrentRuneBuffId();
        return BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData data) && data != null ? data.name : BuffName(buffId);
    }

    private static IReadOnlyDictionary<string, object> WithDefaultArguments(IReadOnlyDictionary<string, object> args)
    {
        if (args != null && args.ContainsKey(RuneBuffArgument))
        {
            return args;
        }

        Dictionary<string, object> merged = args != null ? new Dictionary<string, object>(args.Count + 1) : new Dictionary<string, object>(1);
        if (args != null)
        {
            foreach (KeyValuePair<string, object> entry in args)
            {
                merged[entry.Key] = entry.Value;
            }
        }

        merged[RuneBuffArgument] = GetCurrentRuneBuffName();
        return merged;
    }

    private static string Lookup(string key, string fallback)
    {
        return LocalizationManager.TryGetText(key, out string text) ? text : fallback ?? string.Empty;
    }

    private static string ToKeyId(int id)
    {
        return id.ToString(CultureInfo.InvariantCulture);
    }
}
