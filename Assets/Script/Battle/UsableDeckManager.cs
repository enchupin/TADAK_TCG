using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 사용 가능한 덱을 관리하는 매니저
/// </summary>
public class UsableDeckManager : MonoBehaviour
{
    public Queue<Card> usableDeck;
    public List<Card> discardPile = new List<Card>(); // 버린 카드 더미
    public List<Card> exhaustPile = new List<Card>(); // 소멸 카드 더미

    /// <summary>
    /// 버리기 더미에 카드 추가
    /// </summary>
    public void AddToDiscard(Card card)
    {
        if (card == null) {
            return;
        }

        card = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
            : card;
        discardPile.Add(card);
    }

    /// <summary>
    /// 버리기 더미에 카드 리스트 추가
    /// </summary>
    public void AddToDiscard(List<Card> cards)
    {
        if (cards == null || cards.Count == 0) {
            return;
        }

        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            Card processedCard = TrainingBattleManager.Instance != null
                ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
                : card;
            discardPile.Add(processedCard);
        }
    }

    /// <summary>
    /// 소멸 카드 더미에 카드 추가
    /// </summary>
    public void AddToExhaust(Card card)
    {
        if (card == null) {
            return;
        }

        card = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
            : card;
        exhaustPile.Add(card);
    }

    /// <summary>
    /// 소멸 카드 더미에 카드 리스트 추가
    /// </summary>
    public void AddToExhaust(List<Card> cards)
    {
        if (cards == null || cards.Count == 0) {
            return;
        }

        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            Card processedCard = TrainingBattleManager.Instance != null
                ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
                : card;
            exhaustPile.Add(processedCard);
        }
    }

    /// <summary>
    /// 버린 카드 더미 리스트 반환
    /// </summary>
    public List<Card> GetDiscardPile()
    {
        return new List<Card>(discardPile);
    }

    /// <summary>
    /// 버린 카드 더미에서 특정 카드 제거
    /// </summary>
    public void RemoveFromDiscard(Card card)
    {
        if (card == null) {
            return;
        }

        if (discardPile.Contains(card))
        {
            discardPile.Remove(card);
        }
    }

    public void RemoveFromExhaust(Card card)
    {
        if (card == null)
        {
            return;
        }

        if (exhaustPile.Contains(card))
        {
            exhaustPile.Remove(card);
        }
    }

    public void RemoveFromCombat(Card card)
    {
        if (card == null)
        {
            return;
        }

        RemoveFromDrawPile(card);
        RemoveFromDiscard(card);
        RemoveFromExhaust(card);
    }

    /// <summary>
    /// 소멸 카드 더미 리스트 반환
    /// </summary>
    public List<Card> GetExhaustPile()
    {
        return new List<Card>(exhaustPile);
    }

    /// <summary>
    /// 덱에서 카드 1장을 드로우
    /// </summary>
    public Card DrawCard()
    {
        if (usableDeck == null || usableDeck.Count == 0) {
            // 덱이 비었으면 버린 카드를 섞어 다시 덱으로
            if (discardPile.Count > 0)
            {
                ReshuffleDiscardToDeck();
            }
            else
            {
                Debug.LogWarning("덱과 버린 카드 더미가 모두 비었습니다");
                return null;
            }
        }

        return usableDeck.Dequeue();
    }

    /// <summary>
    /// 덱에서 지정한 수만큼 카드를 드로우
    /// </summary>
    public List<Card> DrawCard(int count)
    {
        List<Card> drawnCards = new List<Card>();

        for (int i = 0; i < count; i++) {
            Card card = DrawCard();
            if (card != null)
            {
                drawnCards.Add(card);
            }
            else
            {
                break; // 더 이상 뽑을 카드가 없음
            }
        }

        return drawnCards;
    }

    /// <summary>
    /// 덱에서 기본 카드만 지정한 수만큼 드로우
    /// </summary>
    public List<Card> DrawBasicCards(int count, Character? characterFilter = null)
    {
        List<Card> drawnCards = new List<Card>();
        if (count <= 0) {
            return drawnCards;
        }

        for (int i = 0; i < count; i++) {
            Card card = DrawBasicCard(characterFilter);
            if (card == null) {
                break;
            }

            drawnCards.Add(card);
        }

        return drawnCards;
    }

    /// <summary>
    /// 덱에서 같은 캐릭터 카드만 지정한 수만큼 드로우
    /// </summary>
    public List<Card> DrawCharacterCards(int count, Character? characterFilter = null)
    {
        List<Card> drawnCards = new List<Card>();
        if (count <= 0 || !characterFilter.HasValue) {
            return drawnCards;
        }

        for (int i = 0; i < count; i++) {
            Card card = DrawCharacterCard(characterFilter.Value);
            if (card == null) {
                break;
            }

            drawnCards.Add(card);
        }

        return drawnCards;
    }

    /// <summary>
    /// 덱에서 기본 카드 1장을 찾아 드로우
    /// </summary>
    private Card DrawBasicCard(Character? characterFilter)
    {
        if (usableDeck == null || usableDeck.Count == 0) {
            if (discardPile.Count > 0) {
                ReshuffleDiscardToDeck();
            }
            else {
                return null;
            }
        }

        if (usableDeck == null || usableDeck.Count == 0) {
            return null;
        }

        List<Card> drawPile = new List<Card>(usableDeck);
        int foundIndex = -1;

        for (int i = 0; i < drawPile.Count; i++) {
            Card candidate = drawPile[i];
            if (candidate != null && IsBasicCardId(candidate.cardId) && (!characterFilter.HasValue || candidate.character == characterFilter.Value)) {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex < 0) {
            return null;
        }

        Card drawnCard = drawPile[foundIndex];
        drawPile.RemoveAt(foundIndex);
        usableDeck = new Queue<Card>(drawPile);
        return drawnCard;
    }

    /// <summary>
    /// 덱에서 같은 캐릭터 카드 1장을 찾아 드로우
    /// </summary>
    private Card DrawCharacterCard(Character characterFilter)
    {
        if (usableDeck == null || usableDeck.Count == 0) {
            if (discardPile.Count > 0) {
                ReshuffleDiscardToDeck();
            }
            else {
                return null;
            }
        }

        if (usableDeck == null || usableDeck.Count == 0) {
            return null;
        }

        List<Card> drawPile = new List<Card>(usableDeck);
        int foundIndex = -1;

        for (int i = 0; i < drawPile.Count; i++) {
            Card candidate = drawPile[i];
            if (candidate != null && candidate.character == characterFilter) {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex < 0) {
            return null;
        }

        Card drawnCard = drawPile[foundIndex];
        drawPile.RemoveAt(foundIndex);
        usableDeck = new Queue<Card>(drawPile);
        return drawnCard;
    }

    private static bool IsBasicCardId(int cardId)
    {
        int suffix = Mathf.Abs(cardId) % 1000;
        return suffix == 10 || suffix == 20;
    }

    public bool RemoveFromDrawPile(Card card)
    {
        if (card == null || usableDeck == null || usableDeck.Count == 0) {
            return false;
        }

        List<Card> drawPile = new List<Card>(usableDeck);
        int index = drawPile.IndexOf(card);
        if (index < 0) {
            return false;
        }

        drawPile.RemoveAt(index);
        usableDeck = new Queue<Card>(drawPile);
        return true;
    }

    public void AddToDrawPileTop(Card card)
    {
        if (card == null) {
            return;
        }

        card = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
            : card;
        List<Card> drawPile = usableDeck != null ? new List<Card>(usableDeck) : new List<Card>();
        drawPile.Insert(0, card);
        usableDeck = new Queue<Card>(drawPile);
    }

    public void AddToDrawPileRandom(Card card)
    {
        if (card == null) {
            return;
        }

        card = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
            : card;
        List<Card> drawPile = usableDeck != null ? new List<Card>(usableDeck) : new List<Card>();
        int index = Random.Range(0, drawPile.Count + 1);
        drawPile.Insert(index, card);
        usableDeck = new Queue<Card>(drawPile);
    }

    /// <summary>
    /// 버린 카드를 덱으로 되돌리고 셔플
    /// </summary>
    private void ReshuffleDiscardToDeck()
    {
        Debug.Log("덱이 비었습니다. 버린 카드를 섞어서 덱으로 만듭니다");
        usableDeck = new Queue<Card>(discardPile);
        discardPile.Clear();
        ShuffleDeck();
    }

    /// <summary>
    /// 덱 셔플
    /// </summary>
    public void ShuffleDeck()
    {
        if (usableDeck == null || usableDeck.Count == 0) {
            return;
        }

        // Queue를 List로 변환하여 셔플
        List<Card> tempList = new List<Card>(usableDeck);

        // Fisher-Yates 셔플 알고리즘
        for (int i = tempList.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        // 다시 Queue로 변환
        usableDeck = new Queue<Card>(tempList);
        TrainingBattleManager.Instance?.battleContext?.OnDeckShuffled();
        Debug.Log("덱을 섞었습니다");
    }

    /// <summary>
    /// 남은 카드 수 반환
    /// </summary>
    public int GetRemainingCardCount()
    {
        return usableDeck != null ? usableDeck.Count : 0;
    }

    public int GetDiscardPileCount()
    {
        return discardPile != null ? discardPile.Count : 0;
    }

    public int GetExhaustPileCount()
    {
        return exhaustPile != null ? exhaustPile.Count : 0;
    }

    /// <summary>
    /// 현재 드로우 더미 순서를 그대로 설정
    /// </summary>
    public void SetDrawPile(List<Card> cards)
    {
        usableDeck = cards != null ? new Queue<Card>(cards) : new Queue<Card>();
    }

    /// <summary>
    /// 외부에서 덱을 설정
    /// </summary>
    public void SetDeck(List<Card> cards)
    {
        usableDeck = new Queue<Card>();

        if (cards != null) {
            foreach (Card card in cards) {
                usableDeck.Enqueue(card);
            }
        }

        discardPile.Clear();
        exhaustPile.Clear();

        Debug.Log($"[UsableDeckManager] 덱 설정 완료: 총 {usableDeck.Count}장");
    }

    /// <summary>
    /// 현재 드로우 더미 리스트 반환
    /// </summary>
    public List<Card> GetDrawPile()
    {
        return usableDeck != null ? new List<Card>(usableDeck) : new List<Card>();
    }
}
