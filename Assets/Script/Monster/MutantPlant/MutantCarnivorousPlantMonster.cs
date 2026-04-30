public class MutantCarnivorousPlantMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 102;
    protected override string MonsterName => "蹂???앹땐?앸Ъ";
    protected override int BaseMaxHp => 40;

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                int biteDamage = PreviewOutgoingDamage(7);
                SetAttackIntent(biteDamage, $"?쇳빐瑜?{biteDamage} ?낇옓?덈떎.");
                SetPlannedPattern(10202, MonsterIntentIconType.Attack);
                break;
            case 1:
                SetIntent("힘을 2 얻습니다.");
                SetPlannedPattern(10201, MonsterIntentIconType.BeneficialEffect);
                break;
            default:
                int thornBiteDamage = PreviewOutgoingDamage(3);
                SetAttackIntent(thornBiteDamage, $"?쇳빐瑜?{thornBiteDamage}??3???낇옓?덈떎. ?쇳빐瑜??낇삍?ㅻ㈃ ?먯긽??踰꾨┛ 移대뱶 ?붾????앹꽦?⑸땲??");
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
                AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 2);
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
