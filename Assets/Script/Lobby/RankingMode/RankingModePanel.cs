using UnityEngine;

public class RankingModePanel : MonoBehaviour
{
    [SerializeField] private RankingModeStarterCardPanel starterCardPanel;
    [SerializeField] private RankingModeSavedDeckPanel savedDeckPanel;

    public void ShowCharacterDeckInfo(int characterId)
    {
        Character character = (Character)characterId;
        ShowStarterCards(character);
        ShowSavedDecks(character);
    }

    private void ShowStarterCards(Character character)
    {
        RankingModeStarterCardPanel panel = ResolveStarterCardPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 기본카드 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowStarterCards(character);
    }

    private void ShowSavedDecks(Character character)
    {
        RankingModeSavedDeckPanel panel = ResolveSavedDeckPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 저장덱 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowSavedDecks(character);
    }

    private RankingModeStarterCardPanel ResolveStarterCardPanel()
    {
        return starterCardPanel;
    }

    private RankingModeSavedDeckPanel ResolveSavedDeckPanel()
    {
        return savedDeckPanel;
    }
}
