using System.Collections.Generic;
using UnityEngine;

public class StoneShieldGolemMonster : StoneGolemMonsterBase
{
    [SerializeField] private int baseMaxHp = 40;

    private bool useSupportPattern;

    public override int MonsterId => 106;
    protected override string MonsterName => "돌 방패 골렘";
    protected override int BaseMaxHp => baseMaxHp;

    protected override void BuildNextAction()
    {
        useSupportPattern = HasOtherLivingStoneGolem();
        if (useSupportPattern)
        {
            SetIntent("적에게 약화를 2 부여합니다. 모든 아군이 보호막을 8 얻습니다.");
            SetPlannedPattern(10601, MonsterIntentIconType.Protection, MonsterIntentIconType.HarmfulEffect);
            return;
        }

        SetAttackIntent(20, "피해를 20 입히고 전투에서 이탈합니다.");
        SetPlannedPattern(10602, MonsterIntentIconType.Bomb);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (useSupportPattern)
        {
            target?.AddBuff(BattleRuntimeDefinitions.WeakBuffId, 2);
            ApplyBarrierToAllAllies();
            return;
        }

        DealDamage(target, 20);
        Kill();
    }

    private void ApplyBarrierToAllAllies()
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
            monster?.AddDefense(8);
        }
    }
}
