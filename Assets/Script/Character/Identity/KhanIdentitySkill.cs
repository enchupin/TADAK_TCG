public sealed class KhanIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        int healAmount = battleManager.playerData.maxHP / 5;
        battleManager.playerData.Heal(healAmount);
        battleManager.playerData.AddDefense(12);
        return true;
    }
}
