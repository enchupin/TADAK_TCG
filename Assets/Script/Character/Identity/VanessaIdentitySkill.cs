using static BattleRuntimeDefinitions;

public sealed class VanessaIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        // v1.0: 연격 2, 득점 3
        battleManager.ApplyBuffToPlayer(CardUseAllEnemiesDamageBuffId, 2);
        battleManager.ApplyBuffToPlayer(ScoreBuffId, 3);
        return true;
    }
}
