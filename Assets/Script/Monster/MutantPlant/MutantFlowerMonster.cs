public class MutantFlowerMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 101;
    protected override string MonsterName => "변이 꽃";
    protected override int BaseMaxHp => 40;

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                int attackDamage = PreviewOutgoingDamage(9);
                SetAttackIntent(attackDamage, $"피해를 {attackDamage} 입힙니다.");
                SetPlannedPattern(10101, MonsterIntentIconType.Attack);
                break;
            case 1:
                int guardedAttackDamage = PreviewOutgoingDamage(6);
                int barrierGain = PreviewBarrierGain(7);
                SetAttackIntent(guardedAttackDamage, $"피해를 {guardedAttackDamage} 입힙니다. 보호막을 {barrierGain} 얻습니다.");
                SetPlannedPattern(10102, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection);
                break;
            default:
                int corrosionAmount = PreviewPlayerDebuffAmount(BattleRuntimeDefinitions.CorrosionBuffId, 2);
                SetIntent($"부식을 {corrosionAmount} 부여합니다.");
                SetPlannedPattern(10103, MonsterIntentIconType.HarmfulEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                DealDamage(target, 9);
                break;
            case 1:
                DealDamage(target, 6);
                AddDefense(7);
                break;
            default:
                AddDebuffToPlayer(target, BattleRuntimeDefinitions.CorrosionBuffId, 2);
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
    }
}
