using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 클래스
/// 순수 C# 객체로 카드의 런타임 상태를 관리
/// </summary>
[System.Serializable]
public enum CardCostType
{
    Energy,
    Barrier,
    Rune,
    // 물결(3066) 중첩으로 비용 지불 (R-83)
    Wave
}

[System.Serializable]
public class Card
{
    // 기본 정보
    public int cardId;
    public string cardName;
    public Character character;
    public int cost;
    public CardCostType costType = CardCostType.Energy;
    public int baseCost;
    public string description;  // 카드 설명

    // 강화 가능한 카드 ID 목록
    public List<int> enforceCardIds = new();
    public List<int> keywords = new();

    // 실행 효과 목록
    public List<ICardEffect> effects = new();
    public List<ICardEffect> onDrawEffects = new();

    // 보존 시 실행 효과 목록
    public List<ICardEffect> keepEffects = new();

    // 손에 남았을 때 턴 종료 시 실행 효과 목록
    public List<ICardEffect> endTurnInHandEffects = new();
    public List<int> boundCardIds = new();
    public List<int> pendingBoundCardIds = new();

    // 조각: 사용 시 본래 효과 다음에 발동할 효과와 그 문구 (카드당 1개, 이번 전투 동안 유지, R-73)
    public List<ICardEffect> fragmentEffects;
    public string fragmentDescription;
    // 조각 문구를 뽑은 카드 (언어 변경 시 그 카드 설명에서 다시 뽑음)
    public int fragmentSourceCardId;
    public bool HasFragment => fragmentEffects != null && fragmentEffects.Count > 0;

    // 호기심: 비용 0, 사용하면 본래 효과 대신 강화 선택 (R-20~22)
    public bool isCurious;
    // 이번 사용이 호기심 강화로 처리되었는지 (행선지를 버린 더미로 고정)
    public bool resolvedAsCuriosity;

    // 이번 사용에 한해 소멸하지 않고 버린 카드 더미로 이동
    public bool skipExhaustOnce;

    // 전투 덱 사본이 가리키는 저장 덱 위치 (생성·복제 카드는 -1)
    public int sourceDeckIndex = -1;

    // R-22a 카드가 스스로 영구적으로 늘린 피해량 (저장 덱에 누적, 보스전 시작 시 초기화)
    public int permanentDamageBonus;

    private int turnCostDelta;
    private bool hasTurnCostOverride;
    private int turnCostOverride;

    // 사용되거나 버려질 때까지 유지되는 비용 변경 (턴이 지나도 손에 있으면 유지)
    private int untilPlayedCostDelta;
    private bool hasUntilPlayedCostOverride;
    private int untilPlayedCostOverride;

    /// <summary>
    /// 런타임 상태 초기화
    /// </summary>
    public void InitializeRuntimeState()
    {
        baseCost = Mathf.Max(0, cost);
        turnCostDelta = 0;
        hasTurnCostOverride = false;
        turnCostOverride = 0;
        ResetUntilPlayedModifiers();
        RecalculateCost();
    }

    /// <summary>
    /// 카드를 사용
    /// </summary>
    public void Play(TrainingBattleManager battlemanager)
    {
        battlemanager?.StartCoroutine(CardEffectSequence.Run(PlaySequence(battlemanager), battlemanager));
    }

    public IEnumerator PlaySequence(TrainingBattleManager battlemanager)
    {
        Debug.Log($"[{cardName}] 카드 사용!");
        PromotePendingBoundCardIds();
        resolvedAsCuriosity = false;
        skipExhaustOnce = false;

        BattleContext context = battlemanager?.battleContext;
        // 동전은 카드 사용마다 새로 던짐
        context?.ResetCoinFlip(this);
        List<Card> previousThis = context?.GetContextCards("ThisCard");
        List<Card> previousSelf = context?.GetContextCards("Self");
        try
        {
            if (battlemanager?.battleContext != null)
            {
                List<Card> contextCards = new List<Card> { this };
                battlemanager.battleContext.SetContextCards("ThisCard", contextCards);
                battlemanager.battleContext.SetContextCards("Self", contextCards);
            }

            if (isCurious)
            {
                // 호기심 카드는 본래 효과 없이 강화만 진행
                if (battlemanager != null)
                {
                    yield return battlemanager.PlayCuriositySequence(this);
                }
                yield break;
            }

            yield return CardEffectSequence.Execute(effects, battlemanager);

            if (boundCardIds != null && boundCardIds.Count > 0)
            {
                List<int> boundCardIdSnapshot = new List<int>(boundCardIds);
                foreach (int boundCardId in boundCardIdSnapshot)
                {
                    Card boundCard = CardManager.GetCardAsCard(boundCardId);
                    if (boundCard == null)
                    {
                        continue;
                    }

                    yield return TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(
                        battlemanager,
                        boundCard,
                        battlemanager != null ? battlemanager.currentTarget : null,
                        resolveDestination: false,
                        triggerPowerEffects: false,
                        allowRepeats: false,
                        applyPostPlayKeywords: false);
                }
            }

            if (HasFragment)
            {
                context?.OnFragmentedCardPlayed();
                yield return CardEffectSequence.Execute(fragmentEffects, battlemanager);
            }

        }
        finally
        {
            context?.SetContextCards("ThisCard", previousThis);
            context?.SetContextCards("Self", previousSelf);
        }
    }

    public bool HasKeepEffect()
    {
        return keepEffects != null && keepEffects.Count > 0;
    }

    public bool HasEndTurnInHandEffect()
    {
        return endTurnInHandEffects != null && endTurnInHandEffects.Count > 0;
    }

    public bool HasKeyword(int keywordId)
    {
        return keywords != null && keywords.Contains(keywordId);
    }

    public void AddKeyword(int keywordId)
    {
        if (keywordId <= 0) {
            return;
        }

        keywords ??= new List<int>();
        if (!keywords.Contains(keywordId)) {
            keywords.Add(keywordId);
        }
    }

    public bool CanBePlayed()
    {
        return !HasKeyword(CardKeywordIds.Unplayable);
    }

    public bool ShouldRetainAtTurnEnd()
    {
        return HasKeyword(CardKeywordIds.Keep) || HasKeepEffect();
    }

    public bool ShouldExhaustWhenPlayed()
    {
        return HasKeyword(CardKeywordIds.Exhaust) || HasKeyword(CardKeywordIds.Ghost);
    }

    public bool ShouldLeaveCombatWhenPlayed()
    {
        return HasKeyword(CardKeywordIds.Power);
    }

    public bool ShouldExhaustAtTurnEnd()
    {
        return HasKeyword(CardKeywordIds.Ghost);
    }

    public Card CloneForRuntimeCopy()
    {
        Card clonedCard = CardManager.GetCardAsCard(cardId);
        if (clonedCard == null) {
            clonedCard = new Card();
        }

        clonedCard.cardId = cardId;
        clonedCard.cardName = cardName;
        clonedCard.character = character;
        clonedCard.costType = costType;
        clonedCard.description = description;
        clonedCard.enforceCardIds = enforceCardIds != null
            ? new List<int>(enforceCardIds)
            : new List<int>();
        clonedCard.keywords = keywords != null
            ? new List<int>(keywords)
            : new List<int>();
        clonedCard.effects = effects != null
            ? new List<ICardEffect>(effects)
            : new List<ICardEffect>();
        clonedCard.onDrawEffects = onDrawEffects != null
            ? new List<ICardEffect>(onDrawEffects)
            : new List<ICardEffect>();
        clonedCard.keepEffects = keepEffects != null
            ? new List<ICardEffect>(keepEffects)
            : new List<ICardEffect>();
        clonedCard.endTurnInHandEffects = endTurnInHandEffects != null
            ? new List<ICardEffect>(endTurnInHandEffects)
            : new List<ICardEffect>();
        clonedCard.boundCardIds = boundCardIds != null
            ? new List<int>(boundCardIds)
            : new List<int>();
        clonedCard.pendingBoundCardIds = pendingBoundCardIds != null
            ? new List<int>(pendingBoundCardIds)
            : new List<int>();
        // 복제 카드는 조각을 이어받지만 호기심과 저장 덱 위치는 이어받지 않음
        clonedCard.fragmentEffects = fragmentEffects != null ? new List<ICardEffect>(fragmentEffects) : null;
        clonedCard.fragmentDescription = fragmentDescription;
        clonedCard.fragmentSourceCardId = fragmentSourceCardId;
        clonedCard.permanentDamageBonus = permanentDamageBonus;
        clonedCard.isCurious = false;
        clonedCard.sourceDeckIndex = -1;
        clonedCard.baseCost = Mathf.Max(0, baseCost);
        clonedCard.cost = Mathf.Max(0, cost);
        clonedCard.turnCostDelta = turnCostDelta;
        clonedCard.hasTurnCostOverride = hasTurnCostOverride;
        clonedCard.turnCostOverride = turnCostOverride;
        clonedCard.untilPlayedCostDelta = untilPlayedCostDelta;
        clonedCard.hasUntilPlayedCostOverride = hasUntilPlayedCostOverride;
        clonedCard.untilPlayedCostOverride = untilPlayedCostOverride;
        clonedCard.RecalculateCost();
        return clonedCard;
    }

    public void ExecuteKeepEffects(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null || !HasKeepEffect()) {
            return;
        }

        List<Card> contextCards = new List<Card> { this };
        battleManager.battleContext.SetContextCards("ThisCard", contextCards);
        battleManager.battleContext.SetContextCards("Self", contextCards);

        foreach (ICardEffect effect in keepEffects) {
            effect?.Execute(battleManager);
        }

        battleManager.battleContext.ClearContextCards("ThisCard");
        battleManager.battleContext.ClearContextCards("Self");
    }

    public void ExecuteOnDrawEffects(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null || onDrawEffects == null || onDrawEffects.Count == 0) {
            return;
        }

        List<Card> contextCards = new List<Card> { this };
        battleManager.battleContext.SetContextCards("ThisCard", contextCards);
        battleManager.battleContext.SetContextCards("Self", contextCards);
        battleManager.battleContext.SetContextCards("DrawnCard", contextCards);

        foreach (ICardEffect effect in onDrawEffects) {
            effect?.Execute(battleManager);
        }

        battleManager.battleContext.ClearContextCards("DrawnCard");
        battleManager.battleContext.ClearContextCards("ThisCard");
        battleManager.battleContext.ClearContextCards("Self");
    }

    public void ExecuteEndTurnInHandEffects(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null || !HasEndTurnInHandEffect()) {
            return;
        }

        List<Card> contextCards = new List<Card> { this };
        battleManager.battleContext.SetContextCards("ThisCard", contextCards);
        battleManager.battleContext.SetContextCards("Self", contextCards);

        foreach (ICardEffect effect in endTurnInHandEffects) {
            effect?.Execute(battleManager);
        }

        battleManager.battleContext.ClearContextCards("ThisCard");
        battleManager.battleContext.ClearContextCards("Self");
    }

    public void ApplyCostModifier(int amount, bool turnOnly)
    {
        if (turnOnly) {
            if (hasTurnCostOverride) {
                turnCostOverride = Mathf.Max(0, turnCostOverride + amount);
            }
            else {
                turnCostDelta += amount;
            }
        }
        else {
            baseCost = Mathf.Max(0, baseCost + amount);
        }

        RecalculateCost();
    }

    public void SetCost(int value, bool turnOnly)
    {
        int normalizedValue = Mathf.Max(0, value);
        if (turnOnly) {
            hasTurnCostOverride = true;
            turnCostOverride = normalizedValue;
        }
        else {
            baseCost = normalizedValue;
        }

        RecalculateCost();
    }

    public static bool IsUntilPlayedDuration(string durationText)
    {
        return string.Equals(durationText, "UntilPlayed", System.StringComparison.OrdinalIgnoreCase);
    }

    // 기간 문자열(Turn/UntilPlayed/없음=전투)에 맞춰 비용 증감
    public void ApplyCostModifier(int amount, string durationText)
    {
        if (IsUntilPlayedDuration(durationText)) {
            if (hasUntilPlayedCostOverride) {
                untilPlayedCostOverride = Mathf.Max(0, untilPlayedCostOverride + amount);
            }
            else {
                untilPlayedCostDelta += amount;
            }

            RecalculateCost();
            return;
        }

        ApplyCostModifier(amount, string.Equals(durationText, "Turn", System.StringComparison.OrdinalIgnoreCase));
    }

    // 기간 문자열(Turn/UntilPlayed/없음=전투)에 맞춰 비용 지정
    public void SetCost(int value, string durationText)
    {
        if (IsUntilPlayedDuration(durationText)) {
            hasUntilPlayedCostOverride = true;
            untilPlayedCostOverride = Mathf.Max(0, value);
            RecalculateCost();
            return;
        }

        SetCost(value, string.Equals(durationText, "Turn", System.StringComparison.OrdinalIgnoreCase));
    }

    // 버린 카드 더미·소멸 더미로 이동할 때 호출
    public void ClearUntilPlayedModifiers()
    {
        if (untilPlayedCostDelta == 0 && !hasUntilPlayedCostOverride) {
            return;
        }

        ResetUntilPlayedModifiers();
        RecalculateCost();
    }

    private void ResetUntilPlayedModifiers()
    {
        untilPlayedCostDelta = 0;
        hasUntilPlayedCostOverride = false;
        untilPlayedCostOverride = 0;
    }

    public void ClearTurnModifiers()
    {
        turnCostDelta = 0;
        hasTurnCostOverride = false;
        turnCostOverride = 0;
        RecalculateCost();

    }

    public void AddPendingBoundCardId(int cardId)
    {
        if (cardId <= 0)
        {
            return;
        }

        pendingBoundCardIds ??= new List<int>();
        pendingBoundCardIds.Add(cardId);
    }

    public void ApplyTemplate(Card templateCard)
    {
        if (templateCard == null) {
            return;
        }

        cardId = templateCard.cardId;
        cardName = templateCard.cardName;
        character = templateCard.character;
        costType = templateCard.costType;
        description = templateCard.description;
        enforceCardIds = templateCard.enforceCardIds != null
            ? new List<int>(templateCard.enforceCardIds)
            : new List<int>();
        keywords = templateCard.keywords != null
            ? new List<int>(templateCard.keywords)
            : new List<int>();
        effects = templateCard.effects != null
            ? new List<ICardEffect>(templateCard.effects)
            : new List<ICardEffect>();
        onDrawEffects = templateCard.onDrawEffects != null
            ? new List<ICardEffect>(templateCard.onDrawEffects)
            : new List<ICardEffect>();
        keepEffects = templateCard.keepEffects != null
            ? new List<ICardEffect>(templateCard.keepEffects)
            : new List<ICardEffect>();
        endTurnInHandEffects = templateCard.endTurnInHandEffects != null
            ? new List<ICardEffect>(templateCard.endTurnInHandEffects)
            : new List<ICardEffect>();
        boundCardIds = new List<int>();
        pendingBoundCardIds = new List<int>();

        baseCost = Mathf.Max(0, templateCard.baseCost);
        turnCostDelta = 0;
        hasTurnCostOverride = false;
        turnCostOverride = 0;
        ResetUntilPlayedModifiers();
        RecalculateCost();
    }

    private void PromotePendingBoundCardIds()
    {
        if (pendingBoundCardIds == null || pendingBoundCardIds.Count == 0)
        {
            return;
        }

        boundCardIds ??= new List<int>();
        boundCardIds.AddRange(pendingBoundCardIds);
        pendingBoundCardIds.Clear();
    }

    private void RecalculateCost()
    {
        // 우선순위: 이번 턴 지정 > 사용 전까지 지정 > 기본 + 증감
        int resolvedCost = hasTurnCostOverride
            ? turnCostOverride
            : hasUntilPlayedCostOverride
                ? untilPlayedCostOverride + turnCostDelta
                : baseCost + untilPlayedCostDelta + turnCostDelta;

        cost = Mathf.Max(0, resolvedCost);
    }
}
