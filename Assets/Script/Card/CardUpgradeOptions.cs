using System.Collections.Generic;

// 강화 계열 중 무작위 선택지 뽑기 (전투 강화 Choose · 호기심 카드 · 휴식 강화 공용 규칙)
public static class CardUpgradeOptions
{
    public static List<Card> PickRandom(IEnumerable<int> upgradeCardIds)
    {
        List<int> upgradeIds = upgradeCardIds != null ? new List<int>(upgradeCardIds) : new List<int>();
        List<Card> options = new List<Card>();
        while (upgradeIds.Count > 0 && options.Count < GameRules.Deck.choiceCount)
        {
            int index = UnityEngine.Random.Range(0, upgradeIds.Count);
            Card option = CardManager.GetCardAsCard(upgradeIds[index]);
            upgradeIds.RemoveAt(index);
            if (option != null)
            {
                options.Add(option);
            }
        }

        return options;
    }
}
