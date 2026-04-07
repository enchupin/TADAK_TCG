using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 능력치 변경 이펙트
/// 현재는 보호막과 버프 스택 변경만 처리
/// </summary>
[System.Serializable]
public class ChangeStatEffect : ICardEffect
{
    public string stat;
    public string change;
    public string subject;
    public int buffId;
    public List<int> buffFilterIds;
    public bool random;
    public int amount;
    public string amountFormula;
    public TargetType target = TargetType.Self;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        ExecuteInternal(battleManager, forwardedAmount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null)
        {
            return;
        }

        int changedAmount = 0;
        string normalizedStat = Normalize(stat);

        switch (normalizedStat)
        {
            case "barrier":
            case "shield":
                changedAmount = ApplyBarrierChange(battleManager, forwardedAmount);
                break;

            case "buff":
                changedAmount = ApplyBuffChange(battleManager, forwardedAmount);
                break;

            case "cardid":
                changedAmount = ApplyCardTransform(battleManager);
                break;

            default:
                Debug.LogWarning($"[ChangeStatEffect] 아직 지원하지 않는 stat입니다: {stat}");
                return;
        }

        if (changedAmount > 0 && onActions != null)
        {
            foreach (ICardEffect onAction in onActions)
            {
                onAction?.Execute(battleManager, changedAmount);
            }
        }

        battleManager.UpdateAllUI();
    }

    private int ApplyCardTransform(TrainingBattleManager battleManager)
    {
        Card sourceCard = ResolveTransformTargetCard(battleManager);
        Card selectedCard = CardEffectRuntimeUtility.ResolveSingleCard(battleManager, null, subject);
        if (sourceCard == null || selectedCard == null)
        {
            return 0;
        }

        int templateCardId = ResolveTransformTemplateCardId(sourceCard, selectedCard);
        Card templateCard = CardManager.GetCardAsCard(templateCardId);
        if (templateCard == null)
        {
            Debug.LogWarning($"[ChangeStatEffect] 변신 대상 카드를 찾을 수 없습니다: {templateCardId}");
            return 0;
        }

        sourceCard.ApplyTemplate(templateCard);
        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, new List<Card> { sourceCard });
        return 1;
    }

    private int ApplyBarrierChange(TrainingBattleManager battleManager, int forwardedAmount)
    {
        int totalChanged = 0;
        string normalizedChange = Normalize(change);

        if ((target == TargetType.Self || target == TargetType.None) && battleManager.playerData != null)
        {
            totalChanged += ApplyBarrierChangeToPlayer(battleManager, normalizedChange, forwardedAmount);
            return totalChanged;
        }

        List<Monster> targets = CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
        foreach (Monster monster in targets)
        {
            totalChanged += ApplyBarrierChangeToMonster(battleManager, monster, normalizedChange, forwardedAmount);
        }

        return totalChanged;
    }

    private int ApplyBarrierChangeToPlayer(TrainingBattleManager battleManager, string normalizedChange, int forwardedAmount)
    {
        int currentBarrier = Mathf.Max(0, battleManager.playerData.defense);
        int changedAmount = ResolveBarrierChangeAmount(currentBarrier, normalizedChange, battleManager, forwardedAmount);
        if (changedAmount <= 0)
        {
            return 0;
        }

        return battleManager.playerData.RemoveDefense(changedAmount);
    }

    private int ApplyBarrierChangeToMonster(TrainingBattleManager battleManager, Monster monster, string normalizedChange, int forwardedAmount)
    {
        if (monster == null || monster.IsDead())
        {
            return 0;
        }

        int currentBarrier = Mathf.Max(0, monster.defense);
        int changedAmount = ResolveBarrierChangeAmount(currentBarrier, normalizedChange, battleManager, forwardedAmount);
        if (changedAmount <= 0)
        {
            return 0;
        }

        return monster.RemoveDefense(changedAmount);
    }

    private int ResolveBarrierChangeAmount(int currentBarrier, string normalizedChange, TrainingBattleManager battleManager, int forwardedAmount)
    {
        switch (normalizedChange)
        {
            case "remove":
                return currentBarrier;

            case "decrease":
                int decreaseAmount = ResolveNumericAmount(battleManager, forwardedAmount);
                return Mathf.Clamp(decreaseAmount, 0, currentBarrier);

            default:
                Debug.LogWarning($"[ChangeStatEffect] 지원하지 않는 보호막 변경 타입입니다: {change}");
                return 0;
        }
    }

    private int ApplyBuffChange(TrainingBattleManager battleManager, int forwardedAmount)
    {
        string normalizedChange = Normalize(change);
        int totalChanged = 0;

        if (target == TargetType.Self || target == TargetType.None)
        {
            totalChanged += ApplyBuffChangeToList(battleManager.playerData?.currentBuffs, normalizedChange, battleManager, forwardedAmount);
            return totalChanged;
        }

        List<Monster> targets = CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
        foreach (Monster monster in targets)
        {
            totalChanged += ApplyBuffChangeToList(monster.currentBuffs, normalizedChange, battleManager, forwardedAmount);
            monster.UpdateUI();
        }

        return totalChanged;
    }

    private int ApplyBuffChangeToList(List<Buff> buffs, string normalizedChange, TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (buffs == null)
        {
            return 0;
        }

        switch (normalizedChange)
        {
            case "remove":
                if (random && buffId <= 0 && buffFilterIds != null && buffFilterIds.Count > 0)
                {
                    int removeCount = ResolveNumericAmount(battleManager, forwardedAmount);
                    if (removeCount <= 0)
                    {
                        removeCount = 1;
                    }

                    int removedBuffEntries = 0;
                    for (int i = 0; i < removeCount; i++)
                    {
                        Buff randomBuff = CardEffectRuntimeUtility.PickRandomBuffByFilters(buffs, buffFilterIds);
                        if (randomBuff == null)
                        {
                            break;
                        }

                        buffs.Remove(randomBuff);
                        removedBuffEntries++;
                    }

                    return removedBuffEntries;
                }

                return CardEffectRuntimeUtility.RemoveBuffStacks(buffs, buffId, ResolveNumericAmount(battleManager, forwardedAmount), true);

            case "decrease":
                return CardEffectRuntimeUtility.RemoveBuffStacks(buffs, buffId, ResolveNumericAmount(battleManager, forwardedAmount), false);

            case "multiply":
                return CardEffectRuntimeUtility.MultiplyBuffStacks(buffs, buffId, ResolveNumericAmount(battleManager, forwardedAmount));

            default:
                Debug.LogWarning($"[ChangeStatEffect] 지원하지 않는 버프 변경 타입입니다: {change}");
                return 0;
        }
    }

    private int ResolveNumericAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private Card ResolveTransformTargetCard(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null)
        {
            return null;
        }

        Card contextCard = battleManager.battleContext.GetContextCard("ThisCard");
        if (contextCard != null)
        {
            return contextCard;
        }

        return battleManager.battleContext.GetLastPlayedCard();
    }

    private int ResolveTransformTemplateCardId(Card sourceCard, Card selectedCard)
    {
        if (sourceCard == null || selectedCard == null)
        {
            return -1;
        }

        int enforceIndex = Mathf.Abs(sourceCard.cardId) % 10;
        if (enforceIndex >= 1 && enforceIndex <= 5 && selectedCard.enforceCardIds != null && selectedCard.enforceCardIds.Count >= enforceIndex)
        {
            return selectedCard.enforceCardIds[enforceIndex - 1];
        }

        return selectedCard.cardId;
    }
}
