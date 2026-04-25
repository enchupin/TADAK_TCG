public sealed class JackIdentitySkill : CharacterIdentitySkill
{
    private static readonly int[] EnhancedJokerCardIds = { 102041, 102042, 102043, 102044, 102045 };

    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        AddRandomCardToHand(battleManager, EnhancedJokerCardIds);
        return true;
    }
}
