public sealed class KhanIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        battleManager.ApplyBuffToPlayer(BattleRuntimeDefinitions.RegenerationBuffId, 6);
        battleManager.playerData.AddDefense(12);
        return true;
    }
}
