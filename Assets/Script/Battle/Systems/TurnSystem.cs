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
        if (replanEnemyActions)
        {
            PlanEnemyNextActions();
        }

        if (battleManager.isDebugMode && turnNumber == 1)
        {
            battleManager.DebugDrawCardsByEffect();
        }
        else
        {
            battleManager.DrawCards(GetTurnStartDrawCount());
        }

        battleManager.SetState(BattleTurnState.PlayerAction);
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
        DiscardRemainingHandCards();

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
        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            monster.PlanNextAction();
        }
    }

    private void DiscardRemainingHandCards()
    {
        if (battleManager.handManager == null || battleManager.usableDeckManager == null)
            return;

        List<Card> remainingCards = battleManager.handManager.GetHandCards();
        if (remainingCards.Count == 0)
        {
            return;
        }

        List<Card> retainedCards = new List<Card>();
        List<Card> discardedCards = new List<Card>();

        foreach (Card card in remainingCards)
        {
            if (card != null && card.HasKeepEffect())
            {
                retainedCards.Add(card);
                continue;
            }

            if (card != null)
            {
                discardedCards.Add(card);
            }
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

        foreach (Card retainedCard in retainedCards)
        {
            retainedCard.ExecuteKeepEffects(battleManager);
            battleManager.handManager.RefreshCardDisplay(retainedCard);
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
