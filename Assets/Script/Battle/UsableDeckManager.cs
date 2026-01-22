using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 사용 가능한 덱을 관리하는 매니저
/// </summary>
public class UsableDeckManager : MonoBehaviour
{
    public static UsableDeckManager Instance { get; private set; }
    private readonly DeckInitializer deckInitializer;






    public List<Card> usableDeck;

    private void Awake()
    {
        Initialize();
    }


    // 싱글톤 패턴
    private void Singleton() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }


    private void Initialize() {
        // 싱글톤
        Singleton();

        // usableDeck 초기화
        if (usableDeck == null) usableDeck = new List<Card>();

        // 저장 덱 불러오기
        deckInitializer.InitializeDeck();
        


    }





}
