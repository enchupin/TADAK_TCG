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
        SetAttackIntent(7, "피해를 7씩 2회 입힙니다.");
        SetPlannedPattern(10801, MonsterIntentIconType.Attack);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        for (int hitIndex = 0; hitIndex < 2; hitIndex++)
        {
            int hpDamage = DealDamage(target, 7);
            if (hpDamage > 0)
            {
                StealMaxHp(target, 1);
            }

            if (target != null && target.IsDead())
            {
                break;
            }
        }
    }

    private void StealMaxHp(PlayerData target, int amount)
    {
        if (target == null || amount <= 0)
        {
            return;
        }

        target.maxHP = Mathf.Max(1, target.maxHP - amount);
        target.hp = Mathf.Min(target.hp, target.maxHP);

        maxHP += amount;
        hp = Mathf.Min(maxHP, hp + amount);

        if (TrainingRunState.IsRunActive)
        {
            TrainingRunState.SetPlayerHealthState(target.hp, target.maxHP);
        }

        UpdateUI();
        TrainingBattleManager.Instance?.UpdateAllUI();
    }
}
