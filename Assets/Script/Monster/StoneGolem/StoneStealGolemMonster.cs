using UnityEngine;

public class StoneStealGolemMonster : StoneGolemMonsterBase
{
    [SerializeField] private int baseMaxHp = 40;

    public override int MonsterId => 108;
    protected override string MonsterName => "돌 훔쳐 골렘";
    protected override int BaseMaxHp => baseMaxHp;

    protected override void OnBattleStart()
    {
        base.OnBattleStart();
        AddBuff(BattleRuntimeDefinitions.MonsterLifeStealBuffId, 1);
    }

    protected override void BuildNextAction()
    {
        int attackDamage = PreviewOutgoingDamage(7);
        SetAttackIntent(attackDamage, $"피해를 {attackDamage}씩 2회 입힙니다.");
        SetPlannedPattern(10801, MonsterIntentIconType.Attack);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        for (int hitIndex = 0; hitIndex < 2; hitIndex++)
        {
            DealDamage(target, 7);
            if (target != null && target.IsDead())
            {
                break;
            }
        }
    }
}
