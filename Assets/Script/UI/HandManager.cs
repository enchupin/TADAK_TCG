using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손패 관리 시스템
/// 카드 UI 생성/제거, 레이아웃 관리
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private GameObject cardUIPrefab;
    
    [Header("레이아웃")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private float cardSpacing = 150f;
    
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
    /// 손패에 카드 추가
    /// </summary>
    public void AddCard(Card card)
    {
        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
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
    /// 손패에서 카드 제거
    /// </summary>
    public void RemoveCard(Card card)
    {
        CardUI targetUI = cardUIList.Find(ui => ui.GetCard() == card);
        
        if (targetUI != null)
        {
            cardUIList.Remove(targetUI);
            Destroy(targetUI.gameObject);
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
        return cardUIList.Count;
    }
}
