using static BattleRuntimeDefinitions;

public sealed class RuneIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        battleManager.ApplyBuffToPlayer(RuneBuffId, 5);
        battleManager.AddTurnEndTriggerRepeat(1);
        return true;
    }
}
