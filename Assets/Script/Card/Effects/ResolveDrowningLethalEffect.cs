using static BattleRuntimeDefinitions;

[System.Serializable]
public sealed class ResolveDrowningLethalEffect : ICardEffect
{
    public int buffId;
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int resolvedBuffId = buffId > 0 ? buffId : DrowningBuffId;
        foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            if (monster.GetBuffStack(resolvedBuffId) > monster.maxHP)
            {
                monster.Kill();
            }
        }

        battleManager.UpdateAllUI();
    }
}
