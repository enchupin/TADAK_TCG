using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "TrainingScene";
    [SerializeField] private int trainingStageCount = 6;
    [SerializeField] private int trainingLaneCount = 3;

    public void ChangeScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }

    public void StartTrainingRun()
    {
        TrainingRunSceneActions.StartTrainingRunAndLoadMap(
            trainingMapSceneName,
            trainingBattleSceneName,
            trainingStageCount,
            trainingLaneCount);
    }
}
