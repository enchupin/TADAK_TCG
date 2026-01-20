using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// 카드 UI 컴포넌트
/// 카드 데이터를 받아서 UI에 표시하고 클릭 이벤트 처리
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 컴포넌트")]
#if UNITY_TEXTMESHPRO
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
#else
    [SerializeField] private Text cardNameText;
    [SerializeField] private Text costText;
    [SerializeField] private Text descriptionText;
#endif
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cardArtwork;
    
    [Header("설정")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unplayableColor = Color.gray;
    
    private Card cardData;
    private BattleManager battleManager;
    private Vector3 originalScale;
    private bool isPlayable = true;
    
    void Awake()
    {
        originalScale = transform.localScale;
    }
    
    /// <summary>
    /// 카드 데이터로 UI 초기화
    /// </summary>
    public void Initialize(Card card, BattleManager manager)
    {
        cardData = card;
        battleManager = manager;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    public void UpdateDisplay()
    {
        if (cardData == null) return;
        
        // 텍스트 업데이트
        if (cardNameText != null)
            cardNameText.text = cardData.cardName;
        
        if (costText != null)
            costText.text = cardData.cost.ToString();
        
        if (descriptionText != null)
            descriptionText.text = GetCardDescription();
        
        // 카드 이미지 (나중에 Addressables로 로드)
        if (cardArtwork != null && cardData.artwork != null)
            cardArtwork.sprite = cardData.artwork;
    }
    
    /// <summary>
    /// 카드 설명 생성
    /// </summary>
    private string GetCardDescription()
    {
        string description = "";
        
        foreach (var effect in cardData.effects)
        {
            if (effect is DamageEffect dmg)
                description += $"데미지 {dmg.amount}\n";
            else if (effect is DefenseEffect def)
                description += $"방어력 {def.amount}\n";
            else if (effect is DrawEffect draw)
                description += $"카드 {draw.amount}장 뽑기\n";
            else if (effect is BuffEffect buff)
                description += $"{buff.stat} +{buff.amount}\n";
            else if (effect is EnergyEffect energy)
                description += $"에너지 +{energy.amount}\n";
            else if (effect is DamagePerCardPlayedEffect combo)
                description += $"데미지 {combo.baseDamage} + 카드당 {combo.bonusPerCard}\n";
            else if (effect is ExecuteDamageEffect exe)
                description += $"처형: 데미지 {exe.baseDamage}\n";
        }
        
        return description.TrimEnd();
    }
    
    /// <summary>
    /// 사용 가능 여부 설정
    /// </summary>
    public void SetPlayable(bool playable)
    {
        isPlayable = playable;
        
        if (backgroundImage != null)
            backgroundImage.color = playable ? normalColor : unplayableColor;
    }
    
    /// <summary>
    /// 마우스 호버 시
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPlayable)
        {
            transform.localScale = originalScale * hoverScale;
            transform.SetAsLastSibling(); // 맨 앞으로
        }
    }
    
    /// <summary>
    /// 마우스 나갈 시
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 클릭 시
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPlayable && battleManager != null && cardData != null)
        {
            battleManager.PlayCard(cardData);
        }
    }
    
    /// <summary>
    /// 카드 데이터 반환
    /// </summary>
    public Card GetCard()
    {
        return cardData;
    }
}
