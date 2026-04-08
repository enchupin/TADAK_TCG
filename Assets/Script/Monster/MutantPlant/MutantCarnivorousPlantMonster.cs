public class MutantCarnivorousPlantMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 102;
    protected override string MonsterName => "변이 식충식물";
    protected override int BaseMaxHp => 40;

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                SetAttackIntent(7, "피해를 7 입힙니다.");
                SetPlannedPattern(10202, MonsterIntentIconType.Attack);
                break;
            case 1:
                SetIntent("피해 증폭을 2 얻습니다.");
                SetPlannedPattern(10201, MonsterIntentIconType.BeneficialEffect);
                break;
            default:
                SetAttackIntent(9, "피해를 3씩 3회 입힙니다. 피해를 입혔다면 자상을 버린 카드 더미에 생성합니다.");
                SetPlannedPattern(10203, MonsterIntentIconType.Attack, MonsterIntentIconType.DisruptCard);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                DealDamage(target, 7);
                break;
            case 1:
                AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 2);
                break;
            default:
                bool shouldGenerateWound = false;
                for (int hitIndex = 0; hitIndex < 3; hitIndex++)
                {
                    int unblockedDamage = DealDamage(target, 3);
                    if (unblockedDamage > 0)
                    {
                        shouldGenerateWound = true;
                    }
                }

                if (shouldGenerateWound)
                {
                    AddCardToPlayerDiscard(CardManager.GetCardAsCard(10));
                }
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
    }
}
