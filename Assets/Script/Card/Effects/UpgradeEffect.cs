using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeEffect : ICardEffect
{
    public string subject;
    public string upgrade;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        if (battleManager?.handManager == null) {
            return;
        }

        Card sourceCard = CardEffectRuntimeUtility.ResolveSingleCard(battleManager, null, subject);
        if (sourceCard == null || sourceCard.enforceCardIds == null || sourceCard.enforceCardIds.Count == 0) {
            Debug.LogWarning("[UpgradeEffect] 강화 가능한 카드가 없습니다");
            return;
        }

        int selectedUpgradeId = ResolveUpgradeCardId(sourceCard.enforceCardIds);
        if (selectedUpgradeId <= 0) {
            return;
        }

        Card upgradedCard = CardManager.GetCardAsCard(selectedUpgradeId);
        if (upgradedCard == null) {
            Debug.LogWarning($"[UpgradeEffect] 강화 카드 생성에 실패했습니다: {selectedUpgradeId}");
            return;
        }

        battleManager.handManager.AddCard(upgradedCard);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private int ResolveUpgradeCardId(List<int> candidateIds)
    {
        if (candidateIds == null || candidateIds.Count == 0) {
            return -1;
        }

        if (!string.Equals(upgrade, "Random", System.StringComparison.OrdinalIgnoreCase)) {
            return candidateIds[0];
        }

        int randomIndex = Random.Range(0, candidateIds.Count);
        return candidateIds[randomIndex];
    }
}
