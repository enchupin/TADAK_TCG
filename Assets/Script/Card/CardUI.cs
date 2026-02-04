using UnityEngine;
using UnityEngine.UI;

using TMPro;

/// <summary>
/// 카드 UI 컴포넌트
/// 카드 데이터를 받아서 UI에 표시하고 클릭 이벤트 처리
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cardArtwork;
    
    [Header("설정")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unplayableColor = Color.gray;

    private bool isPlayable = true;
    public bool IsPlayable => isPlayable;

    /// <summary>
    /// UI 업데이트 - CardController로부터 Card 데이터를 받아서 표시
    /// </summary>
    public void UpdateDisplay(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardUI] Card is null!");
            return;
        }
        
        // 텍스트 업데이트
        if (cardNameText != null)
            cardNameText.text = card.cardName;
        
        if (costText != null)
            costText.text = card.cost.ToString();
        
        if (descriptionText != null)
            descriptionText.text = card.description ?? "";
    }
}
