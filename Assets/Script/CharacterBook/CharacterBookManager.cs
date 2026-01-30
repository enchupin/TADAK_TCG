using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 도감 씬에서 선택된 캐릭터의 카드를 보여주는 매니저
/// HandManager를 사용하여 카드 표시를 관리합니다.
/// </summary>
public class CharacterBookManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandManager handManager; // HandManager 참조
    
    [Header("Debug")]
    [SerializeField] private Character defaultCharacter = Character.Chloe;
    [SerializeField] private bool showOnStart = true;

    private void Start()
    {
        // HandManager 자동 찾기
        if (handManager == null)
        {
            handManager = GetComponent<HandManager>();
        }

        // 초기화 시 기본 캐릭터 카드를 보여줄지 여부
        if (showOnStart)
        {
            ShowCardsByCharacter(defaultCharacter);
        }
    }

    /// <summary>
    /// 특정 캐릭터의 모든 카드를 표시합니다.
    /// </summary>
    public void ShowCardsByCharacter(Character character)
    {
        if (handManager == null)
        {
            Debug.LogError("[CharacterBookManager] HandManager reference is missing!");
            return;
        }

        // 기존 카드 제거
        handManager.ClearHand();

        // CardManager에서 해당 캐릭터의 모든 카드 데이터 가져오기
        List<CardData> cards = CardManager.GetCardsByCharacter(character);
        
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning($"[CharacterBookManager] No cards found for {character}");
            return;
        }

        Debug.Log($"[CharacterBookManager] Showing {cards.Count} cards for {character}");

        // 카드 ID 리스트 생성
        List<int> cardIds = new List<int>();
        foreach (var cardData in cards)
        {
            cardIds.Add(cardData.cardId);
        }

        // HandManager를 통해 카드 표시
        handManager.AddCardById(cardIds);
    }
}
