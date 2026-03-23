using System.Collections.Generic;
using UnityEngine;

public class StoneThrowGolemMonster : StoneGolemMonsterBase
{
    [SerializeField] private int baseMaxHp = 40;

    private bool useSupportPattern;

    public override int MonsterId => 107;
    protected override string MonsterName => "돌 던져 골렘";
    protected override int BaseMaxHp => baseMaxHp;

    protected override void BuildNextAction()
    {
        useSupportPattern = HasOtherLivingStoneGolem();
        if (useSupportPattern)
        {
            SetIntent("적에게 부식을 2 부여합니다. 모든 아군이 피해 증폭을 1 얻습니다.");
            SetPlannedPattern(10701, MonsterIntentIconType.BeneficialEffect, MonsterIntentIconType.HarmfulEffect);
            return;
        }

        SetAttackIntent(12, "피해를 12 입힙니다.");
        SetPlannedPattern(10702, MonsterIntentIconType.Attack);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (useSupportPattern)
        {
            target?.AddBuff(BattleRuntimeDefinitions.CorrosionBuffId, 2);
            ApplyDamageAmplifyToAllAllies();
            return;
        }

        DealDamage(target, 12);
    }

    private void ApplyDamageAmplifyToAllAllies()
    {
        List<Monster> livingMonsters = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.GetLivingMonsters()
            : null;
        if (livingMonsters == null)
        {
            return;
        }

        foreach (Monster monster in livingMonsters)
        {
            monster?.AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 1);
        }
    }
}
