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
                SetAttackIntent(9, "피해를 9 입힙니다.");
                SetPlannedPattern(10101, MonsterIntentIconType.Attack);
                break;
            case 1:
                SetAttackIntent(6, "피해를 6 입힙니다. 보호막을 7 얻습니다.");
                SetPlannedPattern(10102, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection);
                break;
            default:
                SetIntent("부식을 2 부여합니다.");
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
