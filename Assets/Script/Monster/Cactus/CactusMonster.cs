using UnityEngine;

public class CactusMonster : Monster
{
    private int plannedPatternIdForTurn;

    public override int MonsterId => 115;
    protected override string MonsterName => "선인장";
    protected override int BaseMaxHp => 28;

    protected override void OnBattleStart()
    {
        plannedPatternIdForTurn = 0;
        AddThorn(3);
    }

    protected override void BuildNextAction()
    {
        plannedPatternIdForTurn = ShouldUseAttackOnly() || Random.Range(0, 2) == 0 ? 11501 : 11502;

        if (plannedPatternIdForTurn == 11501)
        {
            int attackDamage = PreviewOutgoingDamage(7);
            int barrierGain = PreviewBarrierGain(12);
            SetAttackIntent(attackDamage, $"피해를 {attackDamage} 입힙니다. 보호막을 {barrierGain} 얻습니다.");
            SetPlannedPattern(11501, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection);
            return;
        }

        int thornGain = PreviewMonsterBuffAmount(BattleRuntimeDefinitions.ThornBuffId, GetThornGainAmount(2));
        SetIntent($"가시를 {thornGain} 얻습니다.");
        SetPlannedPattern(11502, MonsterIntentIconType.BeneficialEffect);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (plannedPatternIdForTurn == 11501)
        {
            DealDamage(target, 7);
            AddDefense(12);
            return;
        }

        AddThorn(2);
    }

    private bool ShouldUseAttackOnly()
    {
        return GetBuffStack(BattleRuntimeDefinitions.ThornBuffId) >= 9;
    }

    private void AddThorn(int amount)
    {
        int thornGain = GetThornGainAmount(amount);
        if (thornGain > 0)
        {
            AddBuff(BattleRuntimeDefinitions.ThornBuffId, thornGain);
        }
    }

    private int GetThornGainAmount(int amount)
    {
        return Mathf.Clamp(9 - GetBuffStack(BattleRuntimeDefinitions.ThornBuffId), 0, amount);
    }
}
