using System.Collections.Generic;
using System.Text;
using static BattleRuntimeDefinitions;

/// <summary>
/// 전투 중 버프 상태로 툴팁 문구를 만듭니다
/// </summary>
public static class BuffTooltipTextBuilder
{
    // {동력}: 동력 · 우피: 보호 · 우피: 공격은 중첩을 공유하며 우피 모드에 따라 활성 버프가 바뀜
    private const string RuneDynamicKey = "동력";

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

        return BuildEntry(buff.data, buff.stack, TooltipView.Colors);
    }

    // 족보 툴팁: 족보 설명 아래 보유 중인 족보마다 설명과 진행도
    public static string BuildHandRank(IReadOnlyList<Buff> buffs)
    {
        TooltipColors colors = TooltipView.Colors;
        StringBuilder builder = new();
        if (BuffMetadataDatabase.TryGetBuffData(HandRankBuffId, out BuffData handRankData) && handRankData != null)
        {
            builder.Append(BuildEntry(handRankData, 0, colors));
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
                builder.Append("\n\n");
            }

            builder.Append(BuildEntry(member.data, member.stack, colors));
            string progress = BuildHandRankProgress(memberId, colors);
            if (!string.IsNullOrEmpty(progress))
            {
                builder.Append('\n');
                builder.Append(progress);
            }
        }

        return builder.ToString();
    }

    private static string BuildEntry(BuffData data, int stack, TooltipColors colors)
    {
        return BuffTooltipFormatter.FormatEntry(
            data.name,
            data.description,
            stack,
            BuffData.IsHarmfulBuffId(data.buffId),
            colors,
            TryResolveReference,
            TryResolveDynamicName);
    }

    private static bool TryResolveReference(string buffName, out bool harmful)
    {
        harmful = false;
        if (!BuffMetadataDatabase.TryGetBuffId(buffName, out int buffId))
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

        PlayerData player = PlayerData.Instance;
        int buffId = player != null && player.wuppiMode == WuppiModeState.Guard ? WuppiGuardBuffId
            : player != null && player.wuppiMode == WuppiModeState.Attack ? WuppiAttackBuffId
            : RuneBuffId;
        if (!BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData data) || data == null)
        {
            return false;
        }

        buffName = data.name;
        harmful = BuffData.IsHarmfulBuffId(buffId);
        return true;
    }

    private static string BuildHandRankProgress(int memberId, TooltipColors colors)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        PlayerBuffRuntimeService runtime = battleManager != null ? battleManager.Buffs?.Player : null;
        if (runtime == null)
        {
            return string.Empty;
        }

        // 버프 ID가 상수가 아니라 switch 대신 순서대로 확인
        if (memberId == DoubleJackBuffId && runtime.TryGetScript(memberId, out DoubleJackBuffScript doubleJack))
        {
            string lastCost = doubleJack.LastPaidCost >= 0
                ? $" (직전 비용 {TooltipRichText.Value(doubleJack.LastPaidCost, colors)})"
                : string.Empty;
            return $"진행: 연속 {TooltipRichText.Value(doubleJack.SameCostStreak, colors)}/{TooltipRichText.Value(doubleJack.RequiredSameCostStreak, colors)}{lastCost}";
        }

        if (memberId == HighCostRepeatBuffId && runtime.TryGetScript(memberId, out HighCostRepeatBuffScript highCost))
        {
            return highCost.HasRepeatedThisTurn ? "진행: 이번 턴 발동함" : "진행: 이번 턴 발동 대기";
        }

        if (memberId == StraightBuffId && runtime.TryGetScript(memberId, out StraightBuffScript straight))
        {
            if (straight.HasTriggeredThisTurn)
            {
                return "진행: 이번 턴 발동함";
            }

            List<string> paidCosts = new();
            foreach (int cost in StraightBuffScript.RequiredCostList)
            {
                if (straight.HasPaidCostThisTurn(cost))
                {
                    paidCosts.Add(TooltipRichText.Value(cost, colors));
                }
            }

            string paidText = paidCosts.Count > 0 ? $"비용 {string.Join(" · ", paidCosts)} 사용" : "사용한 비용 없음";
            return $"진행: {paidText} ({TooltipRichText.Value(paidCosts.Count, colors)}/{TooltipRichText.Value(StraightBuffScript.RequiredCostList.Count, colors)})";
        }

        if (memberId == RoyalBuffId && runtime.TryGetScript(memberId, out RoyalBuffScript royal))
        {
            return $"진행: {TooltipRichText.Value(royal.PlayedCharacterCount, colors)}/{TooltipRichText.Value(royal.GetRequiredCharacterCount(battleManager), colors)}명";
        }

        return string.Empty;
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
