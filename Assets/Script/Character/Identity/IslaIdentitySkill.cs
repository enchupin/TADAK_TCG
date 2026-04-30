using static BattleRuntimeDefinitions;

public sealed class IslaIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        battleManager.DrawCards(2);
        battleManager.ApplyBuffToAllEnemies(CorrosionBuffId, 3);
        return true;
    }
}
