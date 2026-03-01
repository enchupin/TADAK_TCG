using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 클래스 (순수 C# 객체, MonoBehaviour 아님)
/// 모든 카드가 이 클래스를 공유합니다.
/// </summary>
[System.Serializable]
public class Card
{
    // 기본 정보
    public int cardId;
    public string cardName;
    public Character character;
    public int cost;
    public string description;  // 카드 설명
    
    // 강화 가능한 카드 ID 목록
    public List<int> enforceCardIds = new();
    
    // 효과 리스트
    public List<ICardEffect> effects = new();
    
    /// <summary>
    /// 카드를 사용
    /// </summary>
    public void Play(TrainingBattleManager battlemanager)
    {
        Debug.Log($"[{cardName}] 카드 사용!");
        
        // 모든 효과 실행
        foreach (var effect in effects)
        {
            effect.Execute(battlemanager);
        }
    }
    

}
