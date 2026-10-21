using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 전투 상태를 담는 컨텍스트 클래스
/// 효과 실행 시 필요한 모든 정보를 제공합니다.
/// </summary>
public class BattleContext
{
    // 카드 사용 통계
    private int cardsPlayedThisCombat;
    private int cardsPlayedThisTurn;
    private readonly List<Card> cardsPlayedThisTurnList = new List<Card>();
    private readonly List<Card> cardsPlayedThisCombatList = new List<Card>();

    // 카드 이동 관련
    public int cardsDiscardedThisTurn;
    public int cardsExhaustedThisTurn;
    public int cardsDrawnThisTurn;
    public int deckShuffleCountThisCombat;

    // 데미지 관련
    public int lastDamageDealt;
    public int totalDamageDealt;
    public int playerCardHpLostThisCombat;
    // 이번 전투에서 플레이어가 회복한 체력
    public int playerHealedThisCombat;

    // 에너지 관련
    public int energySpentThisTurn;

    // 동전: 카드 사용마다 한 번 던진 결과 (같은 사용 안의 여러 동전 효과는 같은 결과)
    private readonly Dictionary<Card, bool> coinResultsByCard = new Dictionary<Card, bool>();
    private System.Random coinRandom = new System.Random();
    public bool LastCoinWasHeads { get; private set; }

    // 이번 턴에 조각된 카드를 사용한 횟수
    public int fragmentedCardsPlayedThisTurn;

    // 직접 사용한 카드가 실제로 지불한 비용 (할인·예고 반영 후)
    private Card lastPaidCard;
    private int lastPaidCost;

    // 보호막 관련
    public int defenseConsumed;

    private readonly HashSet<int> enemyDebuffsAppliedThisTurn = new HashSet<int>();

    // 선택된 카드 (Choice -> Effect 연계용)
    private List<Card> selectedCards = new List<Card>();
    private readonly Dictionary<string, List<Card>> contextCardsBySubject = new Dictionary<string, List<Card>>(System.StringComparer.OrdinalIgnoreCase);

    public void SetSelectedCards(List<Card> cards)
    {
        selectedCards = new List<Card>(cards);
    }

    public List<Card> GetSelectedCards()
    {
        return new List<Card>(selectedCards);
    }

    private void ClearSelectedCards()
    {
        selectedCards.Clear();
    }

    public void SetContextCards(string subject, List<Card> cards)
    {
        if (string.IsNullOrWhiteSpace(subject)) {
            return;
        }

        if (cards == null || cards.Count == 0) {
            contextCardsBySubject.Remove(subject);
            return;
        }

        contextCardsBySubject[subject] = new List<Card>(cards);
    }

    public List<Card> GetContextCards(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) {
            return new List<Card>();
        }

        if (contextCardsBySubject.TryGetValue(subject, out List<Card> cards) && cards != null) {
            return new List<Card>(cards);
        }

        return new List<Card>();
    }

    public Card GetContextCard(string subject)
    {
        List<Card> cards = GetContextCards(subject);
        return cards.Count > 0 ? cards[0] : null;
    }

    public void ClearContextCards(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) {
            return;
        }

        contextCardsBySubject.Remove(subject);
    }

    private void ClearAllContextCards()
    {
        contextCardsBySubject.Clear();
    }

    /// <summary>
    /// 턴 시작 시 호출
    /// </summary>
    public void OnTurnStart()
    {
        cardsPlayedThisTurn = 0;
        cardsPlayedThisTurnList.Clear();
        cardsDiscardedThisTurn = 0;
        cardsExhaustedThisTurn = 0;
        cardsDrawnThisTurn = 0;
        totalDamageDealt = 0;
        defenseConsumed = 0;
        energySpentThisTurn = 0;
        fragmentedCardsPlayedThisTurn = 0;
        enemyDebuffsAppliedThisTurn.Clear();
        ClearSelectedCards();
        ClearAllContextCards();
    }
    
    /// <summary>
    /// 전투 시작 시 호출
    /// </summary>
    public void OnCombatStart()
    {
        cardsPlayedThisCombat = 0;
        cardsPlayedThisCombatList.Clear();
        deckShuffleCountThisCombat = 0;
        totalDamageDealt = 0;
        playerCardHpLostThisCombat = 0;
        playerHealedThisCombat = 0;
        OnTurnStart();
    }

    public void OnCardPlayed(Card card)
    {
        cardsPlayedThisTurn++;
        cardsPlayedThisCombat++;

        if (card != null)
        {
            cardsPlayedThisTurnList.Add(card);
            cardsPlayedThisCombatList.Add(card);
            Debug.Log($"[BattleContext] Card played. id={card.cardId}, turnCount={cardsPlayedThisTurn}, combatCount={cardsPlayedThisCombat}");
        }
    }

    public int GetCardsPlayedThisCombatCount(List<int> targetCardIds)
    {
        if (targetCardIds == null || targetCardIds.Count == 0)
            return cardsPlayedThisCombat;

        int count = 0;
        foreach (Card playedCard in cardsPlayedThisCombatList)
        {
            if (playedCard != null && targetCardIds.Contains(playedCard.cardId))
            {
                count++;
            }
        }

        return count;
    }

    public int GetCardsPlayedThisTurnCount(List<int> targetCardIds)
    {
        if (targetCardIds == null || targetCardIds.Count == 0)
            return cardsPlayedThisTurn;

        int count = 0;
        foreach (Card playedCard in cardsPlayedThisTurnList)
        {
            if (playedCard != null && targetCardIds.Contains(playedCard.cardId))
            {
                count++;
            }
        }

        return count;
    }

    public void OnEnemyDebuffApplied(int buffId)
    {
        if (buffId <= 0)
        {
            return;
        }

        enemyDebuffsAppliedThisTurn.Add(buffId);
    }

    public int GetCardsPlayedThisTurnByCharacter(Character character, bool countOthers)
    {
        int count = 0;
        foreach (Card playedCard in cardsPlayedThisTurnList)
        {
            if (playedCard == null || playedCard.character == Character.Monster)
            {
                continue;
            }

            if ((playedCard.character == character) != countOthers)
            {
                count++;
            }
        }

        return count;
    }

    public int GetEnemyDebuffKindsAppliedThisTurn()
    {
        return enemyDebuffsAppliedThisTurn.Count;
    }

    public bool WasEnemyDebuffAppliedThisTurn(int buffId)
    {
        return buffId > 0 && enemyDebuffsAppliedThisTurn.Contains(buffId);
    }

    public Card GetLastPlayedCard()
    {
        if (cardsPlayedThisTurnList == null || cardsPlayedThisTurnList.Count == 0) {
            return null;
        }

        return cardsPlayedThisTurnList[cardsPlayedThisTurnList.Count - 1];
    }

    public void OnCardsDrawn(int count)
    {
        if (count <= 0)
            return;

        cardsDrawnThisTurn += count;
    }

    public void OnCardsDiscarded(int count)
    {
        if (count <= 0)
            return;

        cardsDiscardedThisTurn += count;
    }

    public void OnCardsExhausted(int count)
    {
        if (count <= 0)
            return;

        cardsExhaustedThisTurn += count;
    }

    public void OnDeckShuffled()
    {
        deckShuffleCountThisCombat++;
    }

    public void OnDamageDealt(int amount)
    {
        if (amount <= 0)
            return;

        lastDamageDealt = amount;
        totalDamageDealt += amount;
    }

    public void OnPlayerCardHpLost(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerCardHpLostThisCombat += amount;
    }

    public void OnPlayerHealed(int amount)
    {
        if (amount > 0)
        {
            playerHealedThisCombat += amount;
        }
    }

    public void OnDefenseConsumed(int amount)
    {
        defenseConsumed += amount;
    }

    // 시드를 지정하면 같은 순서의 동전 결과를 재현 (R-72 시드 RNG)
    public void SetCoinSeed(int seed)
    {
        coinRandom = new System.Random(seed);
    }

    public void ResetCoinFlip(Card card)
    {
        if (card != null)
        {
            coinResultsByCard.Remove(card);
        }
    }

    // 앞면이면 true, 행운이 있으면 앞면으로 확정하고 행운을 소모 (행운은 동전으로만 소모)
    public bool ResolveCoinFlip(Card card, System.Func<bool> tryConsumeLuck)
    {
        if (card != null && coinResultsByCard.TryGetValue(card, out bool cachedResult))
        {
            return cachedResult;
        }

        bool isHeads = tryConsumeLuck != null && tryConsumeLuck();
        if (!isHeads)
        {
            isHeads = coinRandom.Next(2) == 0;
        }

        if (card != null)
        {
            coinResultsByCard[card] = isHeads;
        }

        LastCoinWasHeads = isHeads;
        Debug.Log($"[BattleContext] 동전 결과: {(isHeads ? "앞면" : "뒷면")}");
        return isHeads;
    }

    public void OnFragmentedCardPlayed()
    {
        fragmentedCardsPlayedThisTurn++;
    }

    public void RecordPaidCost(Card card, int paidCost)
    {
        lastPaidCard = card;
        lastPaidCost = paidCost < 0 ? 0 : paidCost;
    }

    // 직접 사용한 카드면 지불한 비용, 유발 사용 등으로 지불 기록이 없으면 카드 비용
    public int GetPaidCost(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        return ReferenceEquals(card, lastPaidCard) ? lastPaidCost : card.cost;
    }

    public void OnEnergySpent(int amount)
    {
        if (amount <= 0)
            return;

        energySpentThisTurn += amount;
    }
}
