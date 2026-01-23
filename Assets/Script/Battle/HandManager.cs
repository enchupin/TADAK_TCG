using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손패 관리 시스템
/// 카드 ID를 정수로 관리하며, UI 생성/제거, 레이아웃 관리
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private GameObject cardUIPrefab;
    
    [Header("레이아웃")]
    [SerializeField] private Transform handContainer;



    // never using
    // [SerializeField] private float cardSpacing = 150f;
    
    private List<int> handCardIds = new List<int>(); // 손패를 카드 ID로 관리
    private List<CardUI> cardUIList = new List<CardUI>();
    private BattleManager battleManager;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
    }
    
    /// <summary>
    /// UsableDeckManager에서 카드를 드로우하여 손패에 추가
    /// </summary>
    public void DrawCards(int count)
    {
        if (UsableDeckManager.Instance == null)
        {
            Debug.LogError("UsableDeckManager가 초기화되지 않았습니다!");
            return;
        }
        
        List<int> drawnCardIds = UsableDeckManager.Instance.DrawCard(count);
        
        foreach (int cardId in drawnCardIds)
        {
            AddCardById(cardId);
        }
    }
    
    /// <summary>
    /// 손패에 카드 ID 추가 (UI 생성)
    /// </summary>
    public void AddCardById(int cardId)
    {
        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }
        
        // 손패에 카드 ID 추가
        handCardIds.Add(cardId);
        
        // CardDatabase에서 카드 정보 가져오기
        Card card = CardDatabase.Instance?.GetCardById(cardId);
        if (card == null)
        {
            Debug.LogWarning($"카드 ID {cardId}를 찾을 수 없습니다!");
            return;
        }
        
        // 카드 UI 생성
        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        
        if (cardUI != null)
        {
            cardUI.Initialize(card, battleManager);
            cardUIList.Add(cardUI);
        }
        
        UpdateLayout();
    }
    
    /// <summary>
    /// 손패에 카드 추가 (기존 호환성 유지)
    /// </summary>
    public void AddCard(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("유효하지 않은 카드입니다!");
            return;
        }
        
        // 카드 ID를 정수로 변환하여 추가
        AddCardById(card.cardId);
    }
    
    /// <summary>
    /// 손패에서 카드 제거
    /// </summary>
    public void RemoveCard(Card card)
    {
        CardUI targetUI = cardUIList.Find(ui => ui.GetCard() == card);
        
        if (targetUI != null)
        {
            // UI에서 제거
            cardUIList.Remove(targetUI);
            Destroy(targetUI.gameObject);
            
            // 손패 데이터에서 제거
            handCardIds.Remove(card.cardId);
            
            UpdateLayout();
        }
    }
    
    /// <summary>
    /// 모든 카드 제거
    /// </summary>
    public void ClearHand()
    {
        foreach (var cardUI in cardUIList)
        {
            if (cardUI != null)
                Destroy(cardUI.gameObject);
        }
        
        cardUIList.Clear();
        handCardIds.Clear();
    }
    
    /// <summary>
    /// 손패 UI 업데이트 (에너지 체크)
    /// </summary>
    public void UpdatePlayableCards(int currentEnergy)
    {
        foreach (var cardUI in cardUIList)
        {
            if (cardUI != null)
            {
                Card card = cardUI.GetCard();
                bool playable = (card.cost <= currentEnergy);
                cardUI.SetPlayable(playable);
            }
        }
    }
    
    /// <summary>
    /// 레이아웃 업데이트
    /// </summary>
    private void UpdateLayout()
    {
        // Horizontal Layout Group이 자동으로 처리
        // 필요시 수동 배치 로직 추가 가능
    }
    
    /// <summary>
    /// 손패 카드 수 반환
    /// </summary>
    public int GetCardCount()
    {
        return handCardIds.Count;
    }
    
    /// <summary>
    /// 손패의 카드 ID 목록 반환
    /// </summary>
    public List<int> GetHandCardIds()
    {
        return new List<int>(handCardIds);
    }
}
