[System.Serializable]
public sealed class IncreaseBeneficialBuffStacksEffect : ICardEffect
{
    public int amount;
    public TargetType target = TargetType.Self;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int increaseAmount = amount > 0 ? amount : 1;
        if (target == TargetType.Self || target == TargetType.None)
        {
            IncreasePlayerBuffs(battleManager, increaseAmount);
            battleManager.UpdateAllUI();
            return;
        }

        foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
        {
            IncreaseMonsterBuffs(monster, increaseAmount);
        }

        battleManager.UpdateAllUI();
    }

    private static void IncreasePlayerBuffs(TrainingBattleManager battleManager, int increaseAmount)
    {
        PlayerData player = battleManager.playerData;
        if (player?.currentBuffs == null)
        {
            return;
        }

        foreach (Buff buff in player.currentBuffs)
        {
            if (!CanIncrease(buff))
            {
                continue;
            }

            buff.stack += increaseAmount;
            WuppiModeRuntimeUtility.HandleDirectBuffStackChange(battleManager, player, buff.data.buffId);
        }

        CreamBuffRuntimeUtility.SyncEnergyOverflow(player);
    }

    private static void IncreaseMonsterBuffs(Monster monster, int increaseAmount)
    {
        if (monster?.currentBuffs == null)
        {
            return;
        }

        foreach (Buff buff in monster.currentBuffs)
        {
            if (!CanIncrease(buff))
            {
                continue;
            }

            buff.stack += increaseAmount;
        }

        monster.UpdateUI();
    }

    private static bool CanIncrease(Buff buff)
    {
        return buff?.data != null
            && buff.stack > 0
            && BuffData.IsBeneficialBuffId(buff.data.buffId)
            && !BuffData.IsNonStackableBuffId(buff.data.buffId);
    }
}
