using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 버린 카드 더미에서 선택하는 효과
/// (예: 버린 카드 1장을 선택해서 손으로 가져오기)
/// </summary>
public class ChoiceDiscardEffect : ICardEffect
{
    public int amount;
    public List<ICardEffect> effects; // 선택된 카드에 적용할 효과 (예: Pickup)
    
    public void Execute(TrainingBattleManager battleManager)
    {
        // 1. 버린 카드 더미 확인
        List<Card> discardPile = battleManager.usableDeckManager.GetDrawPile(); // 아님. GetDiscardPile이어야 함.
        // UsableDeckManager에 GetDiscardPile() 방금 추가했음.
        discardPile = battleManager.usableDeckManager.GetDiscardPile();
        
        if (discardPile.Count == 0)
        {
            Debug.Log("[ChoiceDiscardEffect] 버린 카드 더미가 비었습니다.");
            return;
        }

        // 2. UI 표시 (ChoiceHandEffect와 유사하게 CardDiscoveryUI 사용 가능)
        // 현재 TrainingBattleManager나 BattleUI에 카드 선택 UI 호출 메서드가 필요함.
        // 임시로: 랜덤 선택 후 효과 실행 (UI 구현 전까지)
        // TODO: 카드 선택 UI 연동
        
        Debug.LogWarning("[ChoiceDiscardEffect] UI 미구현으로 인해 임시로 랜덤 선택합니다.");
        
        // n장 선택 (중복 방지 등 로직 필요)
        List<Card> selectedCards = new List<Card>();
        for (int i = 0; i < Mathf.Min(amount, discardPile.Count); i++)
        {
            selectedCards.Add(discardPile[i]); // 일단 앞에서부터 n장
        }
        
        // 3. 선택된 상태 설정
        battleManager.battleContext.SetSelectedCards(selectedCards);
        
        // 4. 후속 효과 실행 (Pickup 등)
        if (effects != null)
        {
            foreach (ICardEffect effect in effects) {
                effect?.Execute(battleManager);
            }
        }
    }
}
