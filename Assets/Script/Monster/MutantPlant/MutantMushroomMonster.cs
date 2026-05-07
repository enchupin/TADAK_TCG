public class MutantMushroomMonster : Monster
{
    private bool useDrainAttack = true;

    public override int MonsterId => 103;
    protected override string MonsterName => "변이 버섯";
    protected override int BaseMaxHp => 27;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.PoisonousMushroomBuffId, 1);
    }

    protected override void BuildNextAction()
    {
        if (useDrainAttack)
        {
            int attackDamage = PreviewOutgoingDamage(7);
            SetAttackIntent(attackDamage, $"피해를 {attackDamage} 입힙니다. 입힌 피해만큼 회복합니다.");
            SetPlannedPattern(10301, MonsterIntentIconType.Attack);
            return;
        }

        int barrierGain = PreviewBarrierGain(12);
        int weakAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.WeakBuffId, 2);
        SetIntent($"보호막을 {barrierGain} 얻습니다. 약화를 {weakAmount} 부여합니다.");
        SetPlannedPattern(10302, MonsterIntentIconType.Protection, MonsterIntentIconType.HarmfulEffect);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (useDrainAttack)
        {
            int unblockedDamage = DealDamage(target, 7);
            if (unblockedDamage > 0)
            {
                Heal(unblockedDamage);
            }
        }
        else
        {
            AddDefense(12);
            AddDebuffToPlayer(target, BattleRuntimeDefinitions.WeakBuffId, 2);
        }

        useDrainAttack = !useDrainAttack;
    }
}
