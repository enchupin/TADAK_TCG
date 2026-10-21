// 카드 캐릭터 기준 아이덴티티 게이지 충전
// operation = Others: 다른 아군 캐릭터들의 게이지를 각각 amount만큼, 그 외: 카드 캐릭터 자신
public class ChargeIdentityEffect : ICardEffect
{
    public int amount = 1;
    public string operation;

    public void Execute(TrainingBattleManager battleManager)
    {
        Card sourceCard = battleManager?.battleContext?.GetContextCard("ThisCard")
            ?? battleManager?.battleContext?.GetLastPlayedCard();
        if (sourceCard == null || amount <= 0)
        {
            return;
        }

        if (string.Equals(operation, "Others", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.AddIdentityGaugeToOtherCharacters(sourceCard.character, amount);
        }
        else
        {
            battleManager.ChargeIdentityGauge(sourceCard.character, amount);
        }

        battleManager.UpdateAllUI();
    }
}
