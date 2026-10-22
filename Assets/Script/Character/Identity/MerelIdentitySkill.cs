using static BattleRuntimeDefinitions;

public sealed class MerelIdentitySkill : CharacterIdentitySkill
{
    private const int EnhancedFeatherCardId = 203081;

    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        // v1.0: 강화된 깃털 카드 2장, 은신 1
        AddRepeatedCardToHand(battleManager, EnhancedFeatherCardId, 2);
        battleManager.ApplyBuffToPlayer(StealthBuffId, 1);
        return true;
    }
}
