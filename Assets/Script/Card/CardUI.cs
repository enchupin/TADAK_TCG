using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI keywordText;
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

        if (keywordText != null)
        {
            string keywordLine = BuildKeywordText(card);
            keywordText.text = keywordLine;
            keywordText.gameObject.SetActive(!string.IsNullOrEmpty(keywordLine));
        }

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

    private static string BuildKeywordText(Card card)
    {
        if (card?.keywords == null || card.keywords.Count == 0) {
            return string.Empty;
        }

        List<string> keywordNames = new();
        foreach (int keywordId in card.keywords)
        {
            string keywordName = GetKeywordDisplayName(keywordId);
            if (string.IsNullOrEmpty(keywordName) || keywordNames.Contains(keywordName)) {
                continue;
            }

            keywordNames.Add($"[{keywordName}]");
        }

        return string.Join(" ", keywordNames);
    }

    private static string GetKeywordDisplayName(int keywordId)
    {
        string keywordName = KeywordDatabase.GetKeywordName(keywordId);
        if (!string.IsNullOrWhiteSpace(keywordName))
        {
            return keywordName;
        }

        return keywordId switch
        {
            CardKeywordIds.Keep => "\uBCF4\uC874",
            CardKeywordIds.Unplayable => "\uC0AC\uC6A9\uBD88\uAC00",
            CardKeywordIds.Exhaust => "\uC18C\uBA78",
            CardKeywordIds.Power => "\uD30C\uC6CC",
            CardKeywordIds.Opening => "\uAC1C\uC2DC",
            CardKeywordIds.Shadow => "\uADF8\uB9BC\uC790",
            CardKeywordIds.Finale => "\uC885\uC5B8",
            CardKeywordIds.Ghost => "\uC720\uB839",
            CardKeywordIds.Unique => "\uC720\uC77C",
            _ => string.Empty
        };
    }
}
