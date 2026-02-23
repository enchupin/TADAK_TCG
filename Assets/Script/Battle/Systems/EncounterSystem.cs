using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles encounter-level checks such as alive enemy tracking and combat end.
/// </summary>
public class EncounterSystem
{
    private readonly TrainingBattleManager battleManager;

    public EncounterSystem(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public List<Monster> GetLivingMonsters()
    {
        CleanupMonsterList();

        List<Monster> alive = new List<Monster>();
        foreach (Monster monster in battleManager.spawnedMonsters)
        {
            if (monster != null && !monster.IsDead())
            {
                alive.Add(monster);
            }
        }

        return alive;
    }

    public bool TryHandleCombatEnd()
    {
        if (battleManager.CurrentTurnState == BattleTurnState.CombatEnd)
            return true;

        if (battleManager.playerData != null && battleManager.playerData.IsDead())
        {
            battleManager.SetState(BattleTurnState.CombatEnd);
            battleManager.UpdateEndTurnButtonState();
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            Debug.Log("[EncounterSystem] Defeat. Player is dead.");
            return true;
        }

        if (GetLivingMonsters().Count == 0)
        {
            battleManager.SetState(BattleTurnState.CombatEnd);
            battleManager.UpdateEndTurnButtonState();
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            Debug.Log("[EncounterSystem] Victory. All enemies are dead.");
            return true;
        }

        return false;
    }

    private void CleanupMonsterList()
    {
        battleManager.spawnedMonsters.RemoveAll(monster => monster == null);
    }
}
