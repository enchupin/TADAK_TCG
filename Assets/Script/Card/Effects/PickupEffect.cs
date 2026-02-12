using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 픽업 효과
/// 버린 카드 더미 등에서 카드를 손으로 가져옵니다.
/// 보통 ChoiceDiscardEffect와 함께 사용됩니다.
/// </summary>
public class PickupEffect : ICardEffect
{
    public void Execute(TrainingBattleManager battleManager)
    {
        // Pickup은 단독으로 실행되기보다 ChoiceDiscard의 선택 결과로 실행되는 경우가 많음
        // 하지만 단독 실행 시(예: 가장 최근 버린 카드 가져오기)를 대비해 로직 구현 가능
        // 현재 JSON 구조상 ChoiceDiscard의 'effect'로 Pickup이 지정됨.
        // ChoiceDiscardEffect에서 선택된 카드를 Context에 담아 Execute를 호출할 것임.
        
        // 1. Context에서 대상 카드 확인 (EventValue or Target)
        // Choice 계열은 보통 선택된 카드를 EventValue나 Context의 Target으로 설정함
        
        // 여기서는 ChoiceDiscardEffect가 선택된 카드를 어떻게 전달하느냐에 따라 다름.
        // 일단 공통 규약: 선택된 카드는 battleManager.battleContext.selectedCards에 있다고 가정
        // 혹은 ChoiceDiscardEffect가 직접 이동 로직을 수행할 수도 있음.
        
        // 만약 Pickup이 "선택된 카드를 손으로"라는 의미라면:
        List<Card> targets = battleManager.battleContext.GetSelectedCards();
        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("[PickupEffect] 선택된 카드가 없습니다.");
            return;
        }
        
        foreach (Card card in targets)
        {
            // 버린 카드 더미에서 제거
            battleManager.usableDeckManager.RemoveFromDiscard(card);
            
            // 손으로 가져옴
            if (battleManager.handManager.GetHandCount() < 10) // 핸드 최대치 체크 필요하면 추가
            {
                battleManager.handManager.AddCard(card);
                Debug.Log($"[PickupEffect] {card.cardName} 카드를 버린 카드 더미에서 손으로 가져왔습니다.");
            }
            else
            {
                Debug.LogWarning("[PickupEffect] 손패가 가득 찼습니다.");
                // 다시 버림? or 소멸? 기획에 따라 다름. 일단은 다시 버림
                battleManager.usableDeckManager.AddToDiscard(card);
            }
        }
        
        // 선택 목록 초기화? (ChoiceEffect가 관리할 수도 있음)
        // battleManager.battleContext.ClearSelectedCards();
    }
}
