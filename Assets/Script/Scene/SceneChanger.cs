using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "TrainingScene";

    public void StartTrainingRun()
    {
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.StartNewRun(
            trainingMapSceneName,
            trainingBattleSceneName);
        SceneManager.LoadScene(trainingMapSceneName);
    }
}
