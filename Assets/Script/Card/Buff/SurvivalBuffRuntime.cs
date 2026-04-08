using UnityEngine;
using static BattleRuntimeDefinitions;

public class SurvivalBuffRuntime
{
    private readonly TrainingBattleManager battleManager;

    public SurvivalBuffRuntime(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public bool TryConsumeSoulProtection()
    {
        PlayerData player = battleManager?.playerData;
        if (player == null || player.hp > 0 || player.GetBuffStack(SoulProtectionBuffId) <= 0)
        {
            return false;
        }

        player.hp = 1;
        player.ConsumeBuffStack(SoulProtectionBuffId, 1);
        Debug.Log("영혼 보호가 발동해 체력 1로 버팁니다");
        return true;
    }
}
