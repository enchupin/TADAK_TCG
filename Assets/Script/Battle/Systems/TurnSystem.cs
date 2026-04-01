using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages turn order and turn-state transitions.
/// </summary>
public class TurnSystem
{
    private readonly TrainingBattleManager battleManager;

    private int turnNumber;
    private int pendingExtraDrawAtTurnStart;
    private int pendingExtraTurns;
    private int pendingExtraTurnEndTriggers;

    public bool IsTurnTransitioning { get; private set; }

    public TurnSystem(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void ResetForCombat()
    {
        turnNumber = 0;
        pendingExtraDrawAtTurnStart = 0;
        pendingExtraTurns = 0;
        pendingExtraTurnEndTriggers = 0;
        IsTurnTransitioning = false;
    }

    public void AddTurnStartDrawModifier(int amount)
    {
        pendingExtraDrawAtTurnStart += amount;
    }

    public void AddExtraTurn(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        pendingExtraTurns += amount;
    }

    public void AddTurnEndTriggerRepeat(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        pendingExtraTurnEndTriggers += amount;
    }

    public bool CanPlayerPlayCard()
    {
        return !IsTurnTransitioning && battleManager.CurrentTurnState == BattleTurnState.PlayerAction;
    }

    public bool CanEndPlayerTurn()
    {
        return !IsTurnTransitioning && battleManager.CurrentTurnState == BattleTurnState.PlayerAction;
    }

    public bool TryEndPlayerTurn()
    {
        if (!CanEndPlayerTurn())
            return false;

        battleManager.StartCoroutine(RunEnemyTurnSequence());
        return true;
    }

    public void ForceEndPlayerTurn()
    {
        if (battleManager.CurrentTurnState == BattleTurnState.CombatEnd || IsTurnTransitioning)
            return;

        if (battleManager.CurrentTurnState == BattleTurnState.PlayerTurnStart ||
            battleManager.CurrentTurnState == BattleTurnState.PlayerAction ||
            battleManager.CurrentTurnState == BattleTurnState.PlayerTurnEnd)
        {
            battleManager.StartCoroutine(RunEnemyTurnSequence());
        }
    }

    public void BeginPlayerTurn(bool replanEnemyActions = true)
    {
        if (battleManager.CurrentTurnState == BattleTurnState.CombatEnd)
            return;

        turnNumber++;
        battleManager.SetState(BattleTurnState.PlayerTurnStart);

        if (battleManager.battleContext == null)
        {
            battleManager.battleContext = new BattleContext();
        }
        battleManager.battleContext.OnTurnStart();

        if (battleManager.playerData != null)
        {
            battleManager.playerData.OnTurnStart();
        }

        battleManager.ApplyPlayerTurnStartEffects();
        battleManager.ProcessPendingMonsterRevives();
        if (replanEnemyActions)
        {
            PlanEnemyNextActions();
        }

        if (battleManager.isDebugMode && turnNumber == 1)
        {
            battleManager.DebugDrawMatchingCards();
        }
        else
        {
            battleManager.DrawCards(GetTurnStartDrawCount(), true);
        }

        battleManager.SetState(BattleTurnState.PlayerAction);
        battleManager.ResolveDeferredTurnStartPowerEffects();
        battleManager.UpdateEndTurnButtonState();
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();

        Debug.Log($"[TurnSystem] Player turn started. Turn: {turnNumber}");
    }

    private IEnumerator RunEnemyTurnSequence()
    {
        if (IsTurnTransitioning)
            yield break;

        IsTurnTransitioning = true;

        battleManager.SetState(BattleTurnState.PlayerTurnEnd);
        battleManager.UpdateEndTurnButtonState();
        battleManager.RefreshHandPlayableState();

        battleManager.ApplyPlayerTurnEndEffects();
        ExpireTurnCardModifiers();
        yield return DiscardRemainingHandCards();

        battleManager.UpdateAllUI();

        if (battleManager.TryHandleCombatEnd())
        {
            IsTurnTransitioning = false;
            yield break;
        }

        if (pendingExtraTurns > 0)
        {
            pendingExtraTurns--;
            IsTurnTransitioning = false;
            BeginPlayerTurn(false);
            yield break;
        }

        battleManager.SetState(BattleTurnState.EnemyTurnStart);
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            monster.OnTurnStart();
        }

        yield return new WaitForSeconds(battleManager.EnemyActionDelay);

        battleManager.SetState(BattleTurnState.EnemyAction);

        List<Monster> enemiesForAction = battleManager.GetLivingMonsters();
        foreach (Monster monster in enemiesForAction)
        {
            if (monster == null || monster.IsDead())
                continue;

            monster.ExecutePlannedAction(battleManager.playerData);
            battleManager.UpdateAllUI();

            if (battleManager.playerData != null && battleManager.playerData.IsDead())
                break;

            yield return new WaitForSeconds(battleManager.EnemyActionDelay);
        }

        if (battleManager.TryHandleCombatEnd())
        {
            IsTurnTransitioning = false;
            yield break;
        }

        battleManager.SetState(BattleTurnState.EnemyTurnEnd);
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            monster.OnTurnEnd();
        }

        yield return new WaitForSeconds(battleManager.EnemyActionDelay);

        if (battleManager.TryHandleCombatEnd())
        {
            IsTurnTransitioning = false;
            yield break;
        }

        IsTurnTransitioning = false;
        BeginPlayerTurn();
    }

    private int GetTurnStartDrawCount()
    {
        int total = Mathf.Max(0, battleManager.drawCardCount + pendingExtraDrawAtTurnStart);
        pendingExtraDrawAtTurnStart = 0;
        return total;
    }

    private void PlanEnemyNextActions()
    {
        battleManager.monsterSpawner?.ResetSummonReservations();

        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            monster.PlanNextAction();
        }
    }

    private IEnumerator DiscardRemainingHandCards()
    {
        if (battleManager.handManager == null || battleManager.usableDeckManager == null)
            yield break;
        List<Card> remainingCards = battleManager.handManager.GetHandCards();
        if (remainingCards.Count == 0)
        {
            yield break;
        }

        foreach (Card card in remainingCards)
        {
            card?.ExecuteEndTurnInHandEffects(battleManager);

            if (battleManager.playerData != null && battleManager.playerData.IsDead())
            {
                yield break;
            }
        }

        List<Card> bonusRetainedCards = new List<Card>();
        int bonusRetainCount = battleManager.GetTurnEndRetainCount();
        if (bonusRetainCount > 0)
        {
            List<Card> selectableCards = new List<Card>();
            foreach (Card card in remainingCards)
            {
                if (card == null || card.ShouldExhaustAtTurnEnd() || card.ShouldRetainAtTurnEnd())
                {
                    continue;
                }

                selectableCards.Add(card);
            }

            yield return SelectTurnEndRetainCards(selectableCards, bonusRetainCount, bonusRetainedCards);
        }

        List<Card> retainedCards = new List<Card>();
        List<Card> discardedCards = new List<Card>();
        List<Card> exhaustedCards = new List<Card>();
        foreach (Card card in remainingCards)
        {
            if (card == null)
            {
                continue;
            }
            if (card.ShouldExhaustAtTurnEnd())
            {
                exhaustedCards.Add(card);
                continue;
            }
            if (card.ShouldRetainAtTurnEnd() || bonusRetainedCards.Contains(card))
            {
                retainedCards.Add(card);
                continue;
            }
            discardedCards.Add(card);
        }
        ExecuteAdditionalTurnEndTriggers(remainingCards, retainedCards);
        if (battleManager.playerData != null && battleManager.playerData.IsDead())
        {
            yield break;
        }
        if (discardedCards.Count > 0)
        {
            battleManager.usableDeckManager.AddToDiscard(discardedCards);
            if (battleManager.battleContext != null)
            {
                battleManager.battleContext.OnCardsDiscarded(discardedCards.Count);
            }
            foreach (Card discardedCard in discardedCards)
            {
                battleManager.handManager.RemoveCard(discardedCard);
            }
        }
        if (exhaustedCards.Count > 0)
        {
            if (battleManager.battleContext != null)
            {
                battleManager.battleContext.OnCardsExhausted(exhaustedCards.Count);
            }
            foreach (Card exhaustedCard in exhaustedCards)
            {
                battleManager.handManager.RemoveCard(exhaustedCard);
            }
        }
        foreach (Card retainedCard in retainedCards)
        {
            retainedCard.ExecuteKeepEffects(battleManager);
            battleManager.handManager.RefreshCardDisplay(retainedCard);
        }
    }

    private void ExecuteAdditionalTurnEndTriggers(List<Card> remainingCards, List<Card> retainedCards)
    {
        if (pendingExtraTurnEndTriggers <= 0)
        {
            return;
        }

        int repeatCount = pendingExtraTurnEndTriggers;
        pendingExtraTurnEndTriggers = 0;

        for (int i = 0; i < repeatCount; i++)
        {
            battleManager.ResolveAdditionalTurnEndTriggers();
            if (battleManager.playerData != null && battleManager.playerData.IsDead())
            {
                return;
            }

            if (remainingCards != null)
            {
                foreach (Card card in remainingCards)
                {
                    card?.ExecuteEndTurnInHandEffects(battleManager);
                    if (battleManager.playerData != null && battleManager.playerData.IsDead())
                    {
                        return;
                    }
                }
            }

            if (retainedCards == null)
            {
                continue;
            }

            foreach (Card retainedCard in retainedCards)
            {
                retainedCard?.ExecuteKeepEffects(battleManager);
                battleManager.handManager?.RefreshCardDisplay(retainedCard);
                if (battleManager.playerData != null && battleManager.playerData.IsDead())
                {
                    return;
                }
            }
        }
    }

    private IEnumerator SelectTurnEndRetainCards(List<Card> selectableCards, int selectCount, List<Card> selectedCards)
    {
        if (selectedCards == null || selectableCards == null || selectableCards.Count == 0 || selectCount <= 0)
        {
            yield break;
        }

        int resolvedCount = Mathf.Min(selectCount, selectableCards.Count);
        bool selectionCompleted = false;
        List<Card> resolvedSelection = new List<Card>();

        bool opened = battleManager.OpenSelectCardPanel(selectableCards, resolvedCount, cards =>
        {
            if (cards != null)
            {
                foreach (Card card in cards)
                {
                    if (card != null && selectableCards.Contains(card) && !resolvedSelection.Contains(card))
                    {
                        resolvedSelection.Add(card);
                    }
                }
            }

            selectionCompleted = true;
        });

        if (opened)
        {
            yield return new WaitUntil(() => selectionCompleted);
        }
        else
        {
            for (int i = 0; i < resolvedCount; i++)
            {
                Card card = selectableCards[i];
                if (card != null && !resolvedSelection.Contains(card))
                {
                    resolvedSelection.Add(card);
                }
            }
        }

        foreach (Card card in resolvedSelection)
        {
            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
            }
        }
    }

    private void ExpireTurnCardModifiers()
    {
        if (battleManager == null)
        {
            return;
        }

        ClearTurnModifiers(battleManager.handManager?.GetHandCards());
        ClearTurnModifiers(battleManager.usableDeckManager?.GetDrawPile());
        ClearTurnModifiers(battleManager.usableDeckManager?.GetDiscardPile());
    }

    private static void ClearTurnModifiers(List<Card> cards)
    {
        if (cards == null)
        {
            return;
        }

        foreach (Card card in cards)
        {
            card?.ClearTurnModifiers();
        }
    }
}

