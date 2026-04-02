using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeSceneController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "LobbyScene";

    private void Start()
    {
        if (!TrainingRunState.IsRunActive)
        {
            ReturnToNextScene();
        }
    }

    public void OnClickEscape()
    {
        if (TrainingBattleManager.buildingDeck != null)
        {
            TrainingRunDeckPersistence.SaveRunDeckOnEventEscape(
                TrainingBattleManager.buildingDeck,
                SelectedButtonControl.selectedCharacterList);
        }

        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.ResetRun();
        ReturnToNextScene();
    }

    public void OnClickCancel()
    {
        if (!string.IsNullOrEmpty(TrainingRunState.MapSceneName))
        {
            SceneManager.LoadScene(TrainingRunState.MapSceneName);
            return;
        }

        ReturnToNextScene();
    }

    private void ReturnToNextScene()
    {
        SceneManager.LoadScene(string.IsNullOrEmpty(nextSceneName) ? "LobbyScene" : nextSceneName);
    }
}
