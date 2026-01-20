using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 클래스 (순수 C# 객체, MonoBehaviour 아님)
/// 모든 카드가 이 클래스를 공유합니다.
/// </summary>
public class Card
{
    // 기본 정보
    public string cardId;
    public string cardName;
    public int cost;
    public string rarity;
    
    // 효과 리스트
    public List<ICardEffect> effects = new List<ICardEffect>();
    
    // Addressables 주소
    public string artworkAddress;
    public string effectAddress;
    public string soundAddress;
    
    // 로드된 에셋들
    public Sprite artwork;
    public GameObject effectPrefab;
    public AudioClip soundClip;
    
    /// <summary>
    /// 카드를 사용합니다.
    /// </summary>
    public void Play(BattleContext context)
    {
        Debug.Log($"[{cardName}] 카드 사용!");
        
        // 모든 효과 실행
        foreach (var effect in effects)
        {
            effect.Execute(context);
        }
        
        // 턴 상태 업데이트
        context.cardsPlayedThisTurn++;
        context.cardsPlayedThisTurnList.Add(this);
    }
    
    /// <summary>
    /// Addressables로 비주얼 에셋을 비동기 로드합니다.
    /// TODO: Addressables 패키지 설치 후 활성화
    /// </summary>
    public void LoadAssetsAsync()
    {
        // Addressables 패키지 설치 후 구현 예정
        Debug.Log($"[{cardName}] 에셋 로딩은 Addressables 설치 후 구현됩니다.");
        
        /* Addressables 설치 후 주석 해제
        // 아트워크 로드
        if (!string.IsNullOrEmpty(artworkAddress))
        {
            var artworkHandle = Addressables.LoadAssetAsync<Sprite>(artworkAddress);
            artwork = await artworkHandle.Task;
        }
        
        // 이펙트 프리팹 로드
        if (!string.IsNullOrEmpty(effectAddress))
        {
            var effectHandle = Addressables.LoadAssetAsync<GameObject>(effectAddress);
            effectPrefab = await effectHandle.Task;
        }
        
        // 사운드 클립 로드
        if (!string.IsNullOrEmpty(soundAddress))
        {
            var soundHandle = Addressables.LoadAssetAsync<AudioClip>(soundAddress);
            soundClip = await soundHandle.Task;
        }
        */
    }
    
    /// <summary>
    /// 카드 설명을 생성합니다.
    /// </summary>
    public string GetDescription()
    {
        // 나중에 효과별로 설명 생성
        return $"Cost: {cost}";
    }
}
