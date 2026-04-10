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

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (player == null || playedCard == null || isRepeatedEffect || stack <= 0 || !BuffCardUtility.HasAttackEffect(playedCard))
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, stack);
    }
}
