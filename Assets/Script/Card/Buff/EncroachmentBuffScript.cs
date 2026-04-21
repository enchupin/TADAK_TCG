using System.Collections.Generic;
using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class EncroachmentBuffScript : PlayerBuffScript
{
    public override int BuffId => EncroachmentBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        Monster targetMonster = SelectRandomLivingMonster(battleManager);
        if (targetMonster != null)
        {
            battleManager.ApplyBuffToMonster(targetMonster, DrowningBuffId, stack);
        }
    }

    private static Monster SelectRandomLivingMonster(TrainingBattleManager battleManager)
    {
        List<Monster> livingMonsters = battleManager?.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return null;
        }

        return livingMonsters[Random.Range(0, livingMonsters.Count)];
    }
}
