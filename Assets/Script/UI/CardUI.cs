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




    
    public Card card;
    private bool isPlayable = true;



    /// <summary>
    /// 카드가 생성될 때 호출
    /// </summary>
    public void InitializeCardUI(int cardId) {
        card = CardDatabase.Instance.GetCardById(cardId);
        UpdateDisplay();
    }


    /// <summary>
    /// UI 업데이트
    /// </summary>
    public void UpdateDisplay()
    {
        if (card == null) return;
        
        // 텍스트 업데이트
        if (cardNameText != null)
            cardNameText.text = card.cardName;
        
        if (costText != null)
            costText.text = card.cost.ToString();
        
        if (descriptionText != null)
            descriptionText.text = GetCardDescription();
        
        // 카드 이미지 (나중에 Addressables로 로드)
        if (cardArtwork != null && card.artwork != null)
            cardArtwork.sprite = card.artwork;
        
        // 레어도에 따른 카드 배경색 설정
        if (backgroundImage != null)
        {
            Color rarityColor = GetRarityColor(card.rarity);
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
        
        foreach (var effect in card.effects)
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
        description += $"\n{GetRarityText(card.rarity)}";
        
        return description.TrimEnd();
    }
    
    /// <summary>
    /// 레어도 텍스트 반환
    /// </summary>
    private string GetRarityText(string rarity)
    {
        if (string.IsNullOrEmpty(rarity)) return "[일반]";

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
        if (string.IsNullOrEmpty(rarity)) return new Color(0.9f, 0.9f, 0.9f);

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
    /// 클릭 시 - 이벤트 발행
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (card == null) {
            Debug.LogWarning("[CardUI] 카드가 null입니다!");
            return;
        }

        if (!isPlayable) {
            Debug.Log($"[CardUI] {card.cardName} - 사용 불가능");
            return;
        }

        Debug.Log($"[CardUI] {card.cardName} 클릭 - 이벤트 발행");
        
        // CardUI 데이터 전달 (자기 자신을 BattleManager에 전달)
        CardClickedEventData cardClickData = new CardClickedEventData(this);
        // 이벤트 발행 (BattleManager가 구독하여 처리)
        CardGameEvents.RaiseCardClicked(cardClickData);
    }
}
