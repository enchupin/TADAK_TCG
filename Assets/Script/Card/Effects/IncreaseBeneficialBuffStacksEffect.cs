[System.Serializable]
public sealed class IncreaseBeneficialBuffStacksEffect : ICardEffect
{
    public int amount;
    public TargetType target = TargetType.Self;
    // 지정 시 해당 버프만 증가 (미오 고유 이로운 효과 등)
    public System.Collections.Generic.List<int> buffFilterIds;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int increaseAmount = amount > 0 ? amount : 1;
        if (target == TargetType.Self || target == TargetType.None)
        {
            IncreasePlayerBuffs(battleManager, increaseAmount, buffFilterIds);
            battleManager.UpdateAllUI();
            return;
        }

        foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
        {
            IncreaseMonsterBuffs(monster, increaseAmount, buffFilterIds);
        }

        battleManager.UpdateAllUI();
    }

    private static void IncreasePlayerBuffs(TrainingBattleManager battleManager, int increaseAmount, System.Collections.Generic.List<int> filter)
    {
        PlayerData player = battleManager.playerData;
        if (player?.currentBuffs == null)
        {
            return;
        }

        foreach (Buff buff in player.currentBuffs)
        {
            if (!CanIncrease(buff) || (filter != null && filter.Count > 0 && !filter.Contains(buff.data.buffId)))
            {
                continue;
            }

            buff.stack += increaseAmount;
            WuppiModeRuntimeUtility.HandleDirectBuffStackChange(battleManager, player, buff.data.buffId);
        }

        CreamBuffRuntimeUtility.SyncEnergyOverflow(player);
    }

    private static void IncreaseMonsterBuffs(Monster monster, int increaseAmount, System.Collections.Generic.List<int> filter)
    {
        if (monster?.currentBuffs == null)
        {
            return;
        }

        foreach (Buff buff in monster.currentBuffs)
        {
            if (!CanIncrease(buff) || (filter != null && filter.Count > 0 && !filter.Contains(buff.data.buffId)))
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
