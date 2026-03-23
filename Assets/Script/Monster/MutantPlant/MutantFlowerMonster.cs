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
                SetAttackIntent(9, "Deal 9 damage");
                SetPlannedPattern(10101, MonsterIntentIconType.Attack);
                break;
            case 1:
                SetAttackIntent(6, "Deal 6 damage. Gain 7 Barrier");
                SetPlannedPattern(10102, MonsterIntentIconType.Protection);
                break;
            default:
                SetIntent("Apply 2 Corrosion to player");
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
                target?.AddBuff(BattleRuntimeDefinitions.CorrosionBuffId, 2);
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
    }
}
