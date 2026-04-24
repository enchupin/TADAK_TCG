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

        AddRepeatedCardToHand(battleManager, EnhancedFeatherCardId, 3);
        battleManager.ApplyBuffToPlayer(PrecisionBuffId, 3);
        return true;
    }
}
