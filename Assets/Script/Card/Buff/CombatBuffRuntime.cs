using static BattleRuntimeDefinitions;

public class CombatBuffRuntime
{
    private readonly TrainingBattleManager battleManager;
    private int attackGainAmplifyThisTurn;

    public CombatBuffRuntime(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void OnTurnStart()
    {
        attackGainAmplifyThisTurn = 0;

        int strengthContract = GetPlayerBuffStack(StrengthContractBuffId);
        if (strengthContract > 0 && battleManager.playerData != null)
        {
            battleManager.playerData.LoseHp(strengthContract);
            if (!battleManager.playerData.IsDead())
            {
                battleManager.ApplyBuffToPlayer(DamageAmplifyBuffId, strengthContract);
            }
        }
    }

    public void RegisterAttackGainAmplifyThisTurn(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        attackGainAmplifyThisTurn += amount;
    }

    public int GetCardBaseDamageBonus(Card sourceCard, bool isAttackEffect)
    {
        if (!isAttackEffect || sourceCard == null || sourceCard.cost != 0)
        {
            return 0;
        }

        return GetPlayerBuffStack(PrecisionBuffId);
    }

    public int ConsumeRepeatCount(Card playedCard, bool isRepeatedEffect)
    {
        if (isRepeatedEffect || playedCard == null || !playedCard.HasKeyword(CardKeywordIds.Power))
        {
            return 0;
        }

        int repeatNextPowerCard = GetPlayerBuffStack(RepeatNextPowerCardBuffId);
        if (repeatNextPowerCard > 0)
        {
            battleManager.playerData?.ConsumeBuffStack(RepeatNextPowerCardBuffId, repeatNextPowerCard);
        }

        return repeatNextPowerCard;
    }

    public void OnCardsExhausted(int exhaustedCount)
    {
        if (exhaustedCount <= 0)
        {
            return;
        }

        int exhaustDrawContract = GetPlayerBuffStack(ExhaustDrawContractBuffId);
        if (exhaustDrawContract > 0)
        {
            battleManager.DrawCards(exhaustedCount * exhaustDrawContract);
        }
    }

    public void OnAttackResolved(Monster targetMonster)
    {
        if (targetMonster == null || battleManager?.playerData == null)
        {
            return;
        }

        if (attackGainAmplifyThisTurn <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(DamageAmplifyBuffId, attackGainAmplifyThisTurn);
        battleManager.ApplyBuffToPlayer(DamageAmplifyDecayBuffId, attackGainAmplifyThisTurn);
    }

    public void OnTurnEnd()
    {
        attackGainAmplifyThisTurn = 0;
    }

    private int GetPlayerBuffStack(int buffId)
    {
        return battleManager?.playerData != null ? battleManager.playerData.GetBuffStack(buffId) : 0;
    }
}
