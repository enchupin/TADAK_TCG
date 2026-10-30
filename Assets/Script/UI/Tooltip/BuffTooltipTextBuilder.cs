using System.Collections.Generic;
using System.Text;
using static BattleRuntimeDefinitions;

/// <summary>
/// 전투 중 버프 상태로 툴팁 문구를 만듭니다
/// </summary>
public static class BuffTooltipTextBuilder
{
    // {동력}: 동력 · 우피: 보호 · 우피: 공격은 중첩을 공유하며 우피 모드에 따라 활성 버프가 바뀜
    // 로컬라이징 표에서는 {runeBuff}로 표기
    private const string RuneDynamicKey = GameText.RuneBuffArgument;
    // 족보 보상 문구 (ko: 카드 1장 · 1회 더 발동 · 에너지 {amount})
    private const string RewardCardKey = "ui.tooltip.handRank.rewardCard";
    private const string RewardRepeatKey = "ui.tooltip.handRank.rewardRepeat";
    private const string RewardEnergyKey = "ui.tooltip.handRank.rewardEnergy";

    // 족보(1028)에 묶여 표시되는 버프 (더블 · 풀하우스 · 스트레이트 · 로열 순서)
    private static readonly int[] HandRankMemberOrder = { DoubleJackBuffId, HighCostRepeatBuffId, StraightBuffId, RoyalBuffId };

    public static bool IsHandRankMember(int buffId)
    {
        return System.Array.IndexOf(HandRankMemberOrder, buffId) >= 0;
    }

    public static string Build(Buff buff)
    {
        if (buff?.data == null)
        {
            return string.Empty;
        }

        return BuildEntry(buff.data, buff.stack, TooltipManager.Colors);
    }

    // 족보 툴팁: 족보 이름 한 줄 아래 보유 중인 족보마다 이름: 조건 → 보상 한 줄
    // 각 족보의 설명은 결국 진행도와 보상이므로 설명 대신 진행도로 표시 (사용자 확인)
    public static string BuildHandRank(IReadOnlyList<Buff> buffs)
    {
        TooltipColors colors = TooltipManager.Colors;
        StringBuilder builder = new();
        if (BuffMetadataDatabase.TryGetBuffData(HandRankBuffId, out BuffData handRankData) && handRankData != null)
        {
            builder.Append(TooltipRichText.EffectName(handRankData.name, false, colors));
        }

        foreach (int memberId in HandRankMemberOrder)
        {
            Buff member = FindActiveBuff(buffs, memberId);
            if (member?.data == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            string progressLine = BuildHandRankProgress(member.data, colors);
            builder.Append(!string.IsNullOrEmpty(progressLine)
                ? progressLine
                : TooltipRichText.EffectName(member.data.name, false, colors));
        }

        return builder.ToString();
    }

    private static string BuildEntry(BuffData data, int stack, TooltipColors colors)
    {
        // 번역문의 복수형({stack, plural, …})만 먼저 풀고 {stack}은 색 처리를 위해 남김
        string description = LocalizationManager.Format(
            data.description,
            new Dictionary<string, object> { { "stack", stack } },
            null,
            false);
        return BuffTooltipFormatter.FormatEntry(
            data.name,
            description,
            stack,
            BuffData.IsHarmfulBuffId(data.buffId),
            colors,
            TryResolveReference,
            TryResolveDynamicName,
            TryResolveBuffId);
    }

    // [buff:id] 참조: 현재 언어 이름 · 극성
    internal static bool TryResolveBuffId(int buffId, out string buffName, out bool harmful)
    {
        buffName = null;
        harmful = false;
        if (!BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData data) || data == null)
        {
            return false;
        }

        buffName = data.name;
        harmful = BuffData.IsHarmfulBuffId(buffId);
        return true;
    }

    // 설명 안의 (버프명) 표기를 버프로 해석, 의도 툴팁도 함께 사용
    internal static bool TryResolveReference(string buffName, out bool harmful)
    {
        harmful = false;
        if (!BuffMetadataDatabase.TryGetBuffIdByAnyName(buffName, out int buffId))
        {
            return false;
        }

        harmful = BuffData.IsHarmfulBuffId(buffId);
        return true;
    }

    private static bool TryResolveDynamicName(string key, out string buffName, out bool harmful)
    {
        buffName = null;
        harmful = false;
        if (key != RuneDynamicKey)
        {
            return false;
        }

        int buffId = GameText.GetCurrentRuneBuffId();
        if (!BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData data) || data == null)
        {
            return false;
        }

        buffName = data.name;
        harmful = BuffData.IsHarmfulBuffId(buffId);
        return true;
    }

    private static string BuildHandRankProgress(BuffData memberData, TooltipColors colors)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        PlayerBuffRuntimeService runtime = battleManager != null ? battleManager.Buffs?.Player : null;
        if (runtime == null)
        {
            return string.Empty;
        }

        int memberId = memberData.buffId;
        string name = memberData.name;
        List<(string text, bool achieved)> conditions = new();

        // 버프 ID가 상수가 아니라 switch 대신 순서대로 확인
        // 진행도에는 비용 단어 없이 숫자만 표시 (사용자 요청)
        // 더블: 같은 비용 카드 2장 연속 → 카드 1장 (반복 발동), 첫 장 전에는 ?
        if (memberId == DoubleJackBuffId && runtime.TryGetScript(memberId, out DoubleJackBuffScript doubleJack))
        {
            bool hasFirst = doubleJack.SameCostStreak > 0 && doubleJack.LastPaidCost >= 0;
            string costText = hasFirst ? doubleJack.LastPaidCost.ToString() : "?";
            for (int i = 0; i < doubleJack.RequiredSameCostStreak; i++)
            {
                conditions.Add((costText, i < doubleJack.SameCostStreak));
            }

            return BuffTooltipFormatter.FormatProgressEntry(name, conditions, new[] { GameText.Ui(RewardCardKey) }, false, colors);
        }

        // 풀하우스: 이번 턴 처음 쓰는 비용 3 이상 카드 → 1회 더 발동 (턴당 1회)
        if (memberId == HighCostRepeatBuffId && runtime.TryGetScript(memberId, out HighCostRepeatBuffScript highCost))
        {
            conditions.Add(("3↑", highCost.HasRepeatedThisTurn));
            return BuffTooltipFormatter.FormatProgressEntry(name, conditions, new[] { GameText.Ui(RewardRepeatKey) }, highCost.HasRepeatedThisTurn, colors);
        }

        // 스트레이트: 비용 1 · 2 · 3 → 에너지 2 · 카드 1장 (턴당 1회)
        if (memberId == StraightBuffId && runtime.TryGetScript(memberId, out StraightBuffScript straight))
        {
            foreach (int cost in StraightBuffScript.RequiredCostList)
            {
                conditions.Add((cost.ToString(), straight.HasPaidCostThisTurn(cost)));
            }

            return BuffTooltipFormatter.FormatProgressEntry(name, conditions, new[] { EnergyReward(2), GameText.Ui(RewardCardKey) }, straight.HasTriggeredThisTurn, colors);
        }

        // 로열: 파티 전원의 카드 사용 → 에너지 1 (반복 발동)
        if (memberId == RoyalBuffId && runtime.TryGetScript(memberId, out RoyalBuffScript royal))
        {
            foreach (CharacterIdentityState state in battleManager.GetIdentityStates())
            {
                if (state != null)
                {
                    conditions.Add((GetLocalizedCharacterName(state.Character), royal.HasPlayedCharacter(state.Character)));
                }
            }

            return BuffTooltipFormatter.FormatProgressEntry(name, conditions, new[] { EnergyReward(1) }, false, colors);
        }

        return string.Empty;
    }

    private static string EnergyReward(int amount)
    {
        return GameText.Ui(RewardEnergyKey, new Dictionary<string, object> { { "amount", amount } });
    }

    // 언어 변경 시 CharacterManager가 갱신하는 현지화 이름 사용
    private static string GetLocalizedCharacterName(Character character)
    {
        CharacterData data = CharacterManager.GetCharacterByEnum(character);
        return data != null && !string.IsNullOrWhiteSpace(data.characterName)
            ? data.characterName
            : character.ToString();
    }

    private static Buff FindActiveBuff(IReadOnlyList<Buff> buffs, int buffId)
    {
        if (buffs == null)
        {
            return null;
        }

        foreach (Buff buff in buffs)
        {
            if (buff?.data != null && buff.data.buffId == buffId && buff.stack != 0)
            {
                return buff;
            }
        }

        return null;
    }
}
