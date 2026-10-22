using static BattleRuntimeDefinitions;

public sealed class PrecisionBuffScript : PlayerBuffScript
{
    public override int BuffId => PrecisionBuffId;

    public override int GetCardBaseDamageBonus(TrainingBattleManager battleManager, PlayerData player, Card sourceCard, bool isAttackEffect, int stack, int currentBonus)
    {
        // v1.0 정밀은 비용 0 공격 카드 대신 모든 깃털 카드의 피해에 더함
        if (!isAttackEffect || stack <= 0 || !BuffCardUtility.IsFeatherCard(sourceCard))
        {
            return currentBonus;
        }

        return currentBonus + stack;
    }
}
