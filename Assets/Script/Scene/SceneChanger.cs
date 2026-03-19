using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "TrainingScene";
    [SerializeField] private int trainingStageCount = 15;
    [SerializeField] private int trainingLaneCount = 4;

    public void StartTrainingRun()
    {
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.StartNewRun(
            trainingMapSceneName,
            trainingBattleSceneName,
            trainingStageCount,
            trainingLaneCount);
        SceneManager.LoadScene(trainingMapSceneName);
    }
}
