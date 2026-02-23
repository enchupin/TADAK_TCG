using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Card UI component.
/// Displays card data and exposes playability visuals.
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cardArtwork;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unplayableColor = Color.gray;

    private bool isPlayable = true;
    public bool IsPlayable => isPlayable;

    /// <summary>
    /// Updates text/icon fields from card data.
    /// </summary>
    public void UpdateDisplay(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardUI] Card is null!");
            return;
        }

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (costText != null)
            costText.text = card.cost.ToString();

        if (descriptionText != null)
            descriptionText.text = card.description ?? string.Empty;

        ApplyPlayableVisual();
    }

    public void SetPlayable(bool playable)
    {
        isPlayable = playable;
        ApplyPlayableVisual();
    }

    private void ApplyPlayableVisual()
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = isPlayable ? normalColor : unplayableColor;
    }
}
