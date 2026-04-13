using static BattleRuntimeDefinitions;

public sealed class AttackBoostBuffScript : PlayerBuffScript
{
    public override int BuffId => AttackBoostBuffId;

    public override int GetCardBaseDamageBonus(TrainingBattleManager battleManager, PlayerData player, Card sourceCard, bool isAttackEffect, int stack, int currentBonus)
    {
        if (!isAttackEffect || sourceCard == null || stack <= 0)
        {
            return currentBonus;
        }

        return currentBonus + stack;
    }
}
