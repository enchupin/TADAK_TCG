using static BattleRuntimeDefinitions;

public sealed class VanessaIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        battleManager.ApplyBuffToPlayer(ThornBuffId, 7);
        return true;
    }
}
