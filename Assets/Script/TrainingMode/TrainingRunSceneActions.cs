using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// High-level scene transitions for training-mode run flow.
/// </summary>
public static class TrainingRunSceneActions
{
    public static void StartTrainingRunAndLoadMap(string mapSceneName, string battleSceneName, int stageCount = 6, int laneCount = 3)
    {
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();

        TrainingRunState.StartNewRun(mapSceneName, battleSceneName, stageCount, laneCount);
        SceneManager.LoadScene(mapSceneName);
    }

    public static void HandleBattleFinished(bool isVictory)
    {
        if (!TrainingRunState.HasMapData)
            return;

        TrainingRunState.CompletePendingNode(isVictory);

        if (!string.IsNullOrEmpty(TrainingRunState.MapSceneName))
        {
            SceneManager.LoadScene(TrainingRunState.MapSceneName);
        }
    }
}
