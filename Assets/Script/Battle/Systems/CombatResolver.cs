using UnityEngine;

/// <summary>
/// Resolves a single card-play pipeline.
/// Handles validation, energy payment, effect execution, and card zone movement.
/// </summary>
public class CombatResolver
{
    private readonly TrainingBattleManager battleManager;

    public CombatResolver(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void TryPlayCard(CardPlayEventData eventData)
    {
        if (!battleManager.CanPlayerPlayCard())
        {
            Debug.LogWarning("[CombatResolver] Cannot play card right now. Not in player action state.");
            return;
        }

        if (eventData == null || eventData.cardController == null || eventData.cardController.Card == null)
        {
            Debug.LogWarning("[CombatResolver] Missing CardController or Card.");
            return;
        }

        CardController controller = eventData.cardController;
        Card playedCard = controller.Card;

        if (battleManager.playerData == null)
            return;

        if (battleManager.playerData.energy < playedCard.cost)
        {
            Debug.LogWarning($"[CombatResolver] Not enough energy for {playedCard.cardName}. Needed: {playedCard.cost}, Current: {battleManager.playerData.energy}");
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            return;
        }

        bool spent = battleManager.playerData.UseEnergy(playedCard.cost);
        if (!spent)
        {
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            return;
        }

        Debug.Log($"[Player] Used card: {playedCard.cardName} (Energy now: {battleManager.playerData.energy})");

        battleManager.battleContext?.OnCardPlayed(playedCard);

        battleManager.currentTarget = eventData.targetMonster;
        playedCard.Play(battleManager);
        battleManager.currentTarget = null;

        if (battleManager.handManager != null)
        {
            battleManager.handManager.RemoveCardFromHand(controller.cardUI);
        }

        if (battleManager.usableDeckManager != null)
        {
            battleManager.usableDeckManager.AddToDiscard(playedCard);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
        battleManager.TryHandleCombatEnd();
    }
}
