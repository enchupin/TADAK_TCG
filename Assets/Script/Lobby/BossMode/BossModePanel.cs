using UnityEngine;

public class BossModePanel : MonoBehaviour
{
    [SerializeField] private BossModeStarterCardPanel starterCardPanel;
    [SerializeField] private DictionarySavedDeckPanel savedDeckPanel;

    public void ShowCharacterDeckInfo(int characterId)
    {
        Character character = (Character)characterId;
        ShowStarterCards(character);
        ShowSavedDecks(character);
    }

    private void ShowStarterCards(Character character)
    {
        BossModeStarterCardPanel panel = ResolveStarterCardPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 기본카드 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowStarterCards(character);
    }

    private void ShowSavedDecks(Character character)
    {
        DictionarySavedDeckPanel panel = ResolveSavedDeckPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 저장덱 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowSavedDecks(character);
    }

    private BossModeStarterCardPanel ResolveStarterCardPanel()
    {
        return starterCardPanel;
    }

    private DictionarySavedDeckPanel ResolveSavedDeckPanel()
    {
        return savedDeckPanel;
    }
}
