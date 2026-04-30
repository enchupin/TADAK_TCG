public sealed class CreamIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager) || identityState == null)
        {
            return false;
        }

        battleManager.AddIdentityGaugeToOtherCharacters(identityState.Character, 3);
        return true;
    }
}
