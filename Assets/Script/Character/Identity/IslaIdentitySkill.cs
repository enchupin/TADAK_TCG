using static BattleRuntimeDefinitions;

public sealed class IslaIdentitySkill : CharacterIdentitySkill
{
    private const int PotionCount = 3;
    private const int CorrosionAmount = 3;

    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        // v1.0: 무작위 포션 3장 제작, 모든 적에게 부식 3
        AddGeneratedCardsToHand(battleManager, BuffCardUtility.CreateRandomCardsFromGroup(BuffCardUtility.BasePotionGroupName, PotionCount));
        battleManager.ApplyBuffToAllEnemies(CorrosionBuffId, CorrosionAmount);
        return true;
    }
}
