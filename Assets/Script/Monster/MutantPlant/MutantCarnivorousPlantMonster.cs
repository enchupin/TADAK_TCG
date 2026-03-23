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
                SetAttackIntent(7, "Deal 7 damage");
                SetPlannedPattern(10202, MonsterIntentIconType.Attack);
                break;
            case 1:
                SetIntent("Gain 2 Damage Amplify");
                SetPlannedPattern(10201, MonsterIntentIconType.BeneficialEffect);
                break;
            default:
                SetAttackIntent(9, "Deal 3 damage 3 times. If unblocked damage is dealt, add Wound to discard pile");
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
                    AddCardToPlayerDiscard(CardManager.GetCardAsCard(BattleRuntimeDefinitions.WoundCardId));
                }
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
    }
}
