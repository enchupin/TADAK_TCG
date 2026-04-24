using static BattleRuntimeDefinitions;

public sealed class IgniaIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        battleManager.ApplyBuffToPlayer(AttackBoostBuffId, 20);
        return true;
    }
}
