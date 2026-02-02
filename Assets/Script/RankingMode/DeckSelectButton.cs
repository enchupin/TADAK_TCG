using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 덱 선택 버튼 스크립트
/// 특정 캐릭터의 특정 덱을 선택하는 버튼
/// </summary>
public class DeckSelectButton : MonoBehaviour
{
    private Character character;
    private DeckData deckData;
    private int deckIndex;

    /// <summary>
    /// 덱 선택 버튼 초기화
    /// </summary>
    public void Initialize(Character character, DeckData deckData, int deckIndex)
    {
        this.character = character;
        this.deckData = deckData;
        this.deckIndex = deckIndex;

        Debug.Log($"[DeckSelectButton] 초기화: {character} - {deckData.deckName} (인덱스: {deckIndex})");
    }

    /// <summary>
    /// 버튼 클릭 시 호출 (Button 컴포넌트의 OnClick 이벤트에 연결)
    /// </summary>
    public void OnButtonClick()
    {
        Debug.Log($"[DeckSelectButton] 덱 선택: {character} - {deckData.deckName}");
        
        // TODO: 선택한 덱으로 랭킹 모드 시작
        // 예: RankingModeManager.StartGame(character, deckData);
    }

    /// <summary>
    /// 덱 데이터 가져오기
    /// </summary>
    public DeckData GetDeckData()
    {
        return deckData;
    }

    /// <summary>
    /// 캐릭터 가져오기
    /// </summary>
    public Character GetCharacter()
    {
        return character;
    }

    /// <summary>
    /// 덱 인덱스 가져오기
    /// </summary>
    public int GetDeckIndex()
    {
        return deckIndex;
    }
}
