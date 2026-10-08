using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "CombatScene";

    public void StartTrainingRun()
    {
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        BossMode.SetMode(false);
        TrainingRunState.StartNewRun(
            trainingMapSceneName,
            trainingBattleSceneName);
        SceneManager.LoadScene(trainingMapSceneName);
    }

}
