using System;
using static BattleRuntimeDefinitions;

public sealed class EfficientBarrierBuffScript : PlayerBuffScript
{
    public override int BuffId => EfficientBarrierBuffId;

    public override int GetTurnStartBarrierLoss(TrainingBattleManager battleManager, PlayerData player, int currentDefense, int stack, int currentLoss)
    {
        if (stack <= 0)
        {
            return currentLoss;
        }

        return Math.Min(currentLoss, 15);
    }
}
