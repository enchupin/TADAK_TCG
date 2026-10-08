using System;

public class InfiniteBossMonster : Monster
{
    public override int MonsterId => 9901;
    protected override string MonsterName => "무한 보스";
    protected override string ImageName => "WoodenPuppet";
    protected override int BaseMaxHp => 1000;
    protected override bool IsBossMonster => true;
    public override bool HasInfiniteHealth => true;
    protected override string HealthLabel => $"HP : ∞\n누적 피해: {BossModeSession.TotalDamage:N0}";
    private int completedTurns;
    private int AttackDamage => (int)Math.Min(int.MaxValue, 5L + completedTurns * 2L);

    protected override void BuildNextAction()
    {
        int damage = PreviewOutgoingDamage(AttackDamage);
        SetAttackIntent(damage, $"플레이어에게 피해 {damage}을 입힙니다");
    }

    protected override void ExecuteAction(PlayerData target)
    {
        DealDamage(target, AttackDamage);
    }

    protected override void OnTurnEnded()
    {
        if (completedTurns < int.MaxValue) completedTurns++;
    }

    protected override bool CanDie() => false;
}
