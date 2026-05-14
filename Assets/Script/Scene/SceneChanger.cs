using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "TrainingScene";

    public void StartTrainingRun()
    {
        RankingModeSession.ResetSession();
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.StartNewRun(
            trainingMapSceneName,
            trainingBattleSceneName);
        SceneManager.LoadScene(trainingMapSceneName);
    }

    public void StartRankingRun()
    {
        Debug.LogWarning("[SceneChanger] 랭킹모드는 RankingModeManager에서 저장덱 3개를 선택한 뒤 시작해야 합니다");
    }
}
