using static BattleRuntimeDefinitions;

// 3071 계약: 피: 파티가 카드로 체력을 잃을 때마다 힘 {stack} (R-84)
public sealed class BloodContractBuffScript : PlayerBuffScript
{
    public override int BuffId => BloodContractBuffId;

    public override void OnPlayerCardHpLost(TrainingBattleManager battleManager, PlayerData player, int hpLoss, int stack)
    {
        if (battleManager == null || hpLoss <= 0 || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(StrengthBuffId, stack);
    }
}
