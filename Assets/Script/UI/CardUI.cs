using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 카드 UI 컴포넌트
/// 카드 데이터를 받아서 UI에 표시하고 클릭 이벤트 처리
/// 호버 효과는 UIHoverEffect 컴포넌트가 담당
/// </summary>
public class CardUI : MonoBehaviour, IPointerClickHandler
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
    
    private Card cardData;
    private BattleManager battleManager;
    private bool isPlayable = true;
    
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
        
        // 레어도에 따른 카드 배경색 설정
        if (backgroundImage != null)
        {
            Color rarityColor = GetRarityColor(cardData.rarity);
            backgroundImage.color = rarityColor;
            normalColor = rarityColor;  // 정상 색상도 업데이트
        }
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
            {
                string target = dmg.target == TargetType.AllEnemies ? "모든 적에게" : "적에게";
                description += $"{target} 데미지 {dmg.amount}\n";
            }
            else if (effect is DefenseEffect def)
                description += $"방어력 +{def.amount}\n";
            else if (effect is DrawEffect draw)
                description += $"카드 {draw.amount}장 뽑기\n";
            else if (effect is BuffEffect buff)
                description += $"{buff.stat} +{buff.amount}\n";
            else if (effect is EnergyEffect energy)
                description += $"에너지 +{energy.amount}\n";
            else if (effect is DamagePerCardPlayedEffect combo)
                description += $"데미지 {combo.baseDamage}\n카드당 +{combo.bonusPerCard}\n";
            else if (effect is ExecuteDamageEffect exe)
            {
                int threshold = Mathf.RoundToInt(exe.hpThreshold * 100);
                description += $"처형 (HP {threshold}% 이하)\n데미지 {exe.baseDamage}×{exe.multiplier}\n";
            }
        }
        
        // 레어도 추가
        description += $"\n{GetRarityText(cardData.rarity)}";
        
        return description.TrimEnd();
    }
    
    /// <summary>
    /// 레어도 텍스트 반환
    /// </summary>
    private string GetRarityText(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return "[일반]";
            case "uncommon": return "[고급]";
            case "rare": return "[희귀]";
            case "epic": return "[영웅]";
            case "legendary": return "[전설]";
            default: return "[일반]";
        }
    }
    
    /// <summary>
    /// 레어도에 따른 카드 배경색 반환
    /// </summary>
    private Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return new Color(0.9f, 0.9f, 0.9f);      // 밝은 회색
            case "uncommon": return new Color(0.7f, 0.9f, 0.7f);    // 연한 녹색
            case "rare": return new Color(0.7f, 0.7f, 1f);          // 연한 파란색
            case "epic": return new Color(0.9f, 0.7f, 0.9f);        // 연한 보라색
            case "legendary": return new Color(1f, 0.85f, 0.5f);    // 연한 금색
            default: return Color.white;
        }
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
    
    // 호버 효과는 UIHoverEffect 컴포넌트가 담당
    
    /// <summary>
    /// 클릭 시
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[CardUI] 클릭됨: {cardData?.cardName ?? "null"} (Playable: {isPlayable}, Manager: {battleManager != null})");
        
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
