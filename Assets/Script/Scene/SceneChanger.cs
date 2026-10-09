using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingBattleSceneName = "CombatScene";

    public void StartTrainingRun()
    {
        BossModeSession.Reset();
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.StartNewRun(trainingBattleSceneName);
        SceneManager.LoadScene(trainingBattleSceneName);
    }

}
