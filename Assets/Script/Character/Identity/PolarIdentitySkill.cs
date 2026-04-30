using static BattleRuntimeDefinitions;

public sealed class PolarIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        battleManager.playerData.AddDefense(10);
        battleManager.ApplyBuffToAllEnemies(FreezeBuffId, 2);
        return true;
    }
}
