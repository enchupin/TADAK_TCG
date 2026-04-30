public sealed class OverheatMonsterBuffScript : MonsterBuffScript
{
    private const int BuffIdValue = 3017;

    public override int BuffId => BuffIdValue;

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        return currentMultiplier * (1f + (stack * 0.1f));
    }
}
