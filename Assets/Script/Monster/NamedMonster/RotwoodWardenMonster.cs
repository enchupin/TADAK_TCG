public class RotwoodWardenMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 201;
    protected override string MonsterName => "썩은나무 숲지기";
    protected override int BaseMaxHp => 300;

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                SetIntent("체력을 15 회복합니다. 적에게 빈약을 3 부여합니다.");
                SetPlannedPattern(20101, MonsterIntentIconType.Heal, MonsterIntentIconType.HarmfulEffect);
                break;
            case 1:
                SetIntent("체력을 15 회복합니다. 적에게 드로우 방해를 1 부여합니다.");
                SetPlannedPattern(20102, MonsterIntentIconType.Heal, MonsterIntentIconType.HarmfulEffect);
                break;
            case 2:
                SetIntent("충전 중입니다.");
                SetPlannedPattern(20103, MonsterIntentIconType.Stun);
                break;
            default:
                SetAttackIntent(45, "피해를 45 입힙니다.");
                SetPlannedPattern(20104, MonsterIntentIconType.Attack);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                Heal(15);
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 3);
                break;
            case 1:
                Heal(15);
                target?.AddBuff(BattleRuntimeDefinitions.DrawInterferenceBuffId, 1);
                break;
            case 2:
                break;
            default:
                DealDamage(target, 45);
                break;
        }

        patternIndex = (patternIndex + 1) % 4;
    }
}
