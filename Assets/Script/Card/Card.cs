using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 클래스
/// 순수 C# 객체로 카드의 런타임 상태를 관리
/// </summary>
[System.Serializable]
public class Card
{
    // 기본 정보
    public int cardId;
    public string cardName;
    public Character character;
    public int cost;
    public int baseCost;
    public string description;  // 카드 설명

    // 강화 가능한 카드 ID 목록
    public List<int> enforceCardIds = new();

    // 실행 효과 목록
    public List<ICardEffect> effects = new();

    // 보존 시 실행 효과 목록
    public List<ICardEffect> keepEffects = new();

    private int turnCostDelta;
    private bool hasTurnCostOverride;
    private int turnCostOverride;

    /// <summary>
    /// 런타임 상태 초기화
    /// </summary>
    public void InitializeRuntimeState()
    {
        baseCost = Mathf.Max(0, cost);
        turnCostDelta = 0;
        hasTurnCostOverride = false;
        turnCostOverride = 0;
        RecalculateCost();
    }

    /// <summary>
    /// 카드를 사용
    /// </summary>
    public void Play(TrainingBattleManager battlemanager)
    {
        Debug.Log($"[{cardName}] 카드 사용!");

        foreach (ICardEffect effect in effects)
        {
            effect.Execute(battlemanager);
        }
    }

    public bool HasKeepEffect()
    {
        return keepEffects != null && keepEffects.Count > 0;
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

    public void ClearTurnModifiers()
    {
        turnCostDelta = 0;
        hasTurnCostOverride = false;
        turnCostOverride = 0;
        RecalculateCost();
    }

    public void ApplyTemplate(Card templateCard)
    {
        if (templateCard == null) {
            return;
        }

        cardId = templateCard.cardId;
        cardName = templateCard.cardName;
        character = templateCard.character;
        description = templateCard.description;
        enforceCardIds = templateCard.enforceCardIds != null
            ? new List<int>(templateCard.enforceCardIds)
            : new List<int>();
        effects = templateCard.effects != null
            ? new List<ICardEffect>(templateCard.effects)
            : new List<ICardEffect>();
        keepEffects = templateCard.keepEffects != null
            ? new List<ICardEffect>(templateCard.keepEffects)
            : new List<ICardEffect>();

        baseCost = Mathf.Max(0, templateCard.baseCost);
        turnCostDelta = 0;
        hasTurnCostOverride = false;
        turnCostOverride = 0;
        RecalculateCost();
    }

    private void RecalculateCost()
    {
        int resolvedCost = hasTurnCostOverride
            ? turnCostOverride
            : baseCost + turnCostDelta;

        cost = Mathf.Max(0, resolvedCost);
    }
}
