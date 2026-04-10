using System.Collections.Generic;
using UnityEngine;

public class StoneThrowGolemMonster : StoneGolemMonsterBase
{
    [SerializeField] private int baseMaxHp = 40;

    private bool useSupportPattern;

    public override int MonsterId => 107;
    protected override string MonsterName => "???섏졇 怨⑤젞";
    protected override int BaseMaxHp => baseMaxHp;

    protected override void BuildNextAction()
    {
        useSupportPattern = HasOtherLivingStoneGolem();
        if (useSupportPattern)
        {
            SetIntent("적에게 부식을 2 부여합니다. 모든 아군이 힘을 1 얻습니다.");
            SetPlannedPattern(10701, MonsterIntentIconType.BeneficialEffect, MonsterIntentIconType.HarmfulEffect);
            return;
        }

        int previewDamage = PreviewOutgoingDamage(12);
        SetAttackIntent(previewDamage, $"?쇳빐瑜?{previewDamage} ?낇옓?덈떎.");
        SetPlannedPattern(10702, MonsterIntentIconType.Attack);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (useSupportPattern)
        {
            target?.AddBuff(BattleRuntimeDefinitions.CorrosionBuffId, 2);
            ApplyStrengthToAllAllies();
            return;
        }

        DealDamage(target, 12);
    }

    private void ApplyStrengthToAllAllies()
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
            monster?.AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 1);
        }
    }
}
