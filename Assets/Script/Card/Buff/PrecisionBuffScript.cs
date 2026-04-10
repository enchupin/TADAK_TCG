using static BattleRuntimeDefinitions;

public sealed class PrecisionBuffScript : PlayerBuffScript
{
    public override int BuffId => PrecisionBuffId;

    public override int GetCardBaseDamageBonus(TrainingBattleManager battleManager, PlayerData player, Card sourceCard, bool isAttackEffect, int stack, int currentBonus)
    {
        if (!isAttackEffect || sourceCard == null || sourceCard.cost != 0 || stack <= 0)
        {
            return currentBonus;
        }

        return currentBonus + stack;
    }
}
