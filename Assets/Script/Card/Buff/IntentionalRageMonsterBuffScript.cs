using static BattleRuntimeDefinitions;

public sealed class IntentionalRageMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => IntentionalRageBuffId;

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        return stack > 0 ? currentMultiplier * 2f : currentMultiplier;
    }
}
