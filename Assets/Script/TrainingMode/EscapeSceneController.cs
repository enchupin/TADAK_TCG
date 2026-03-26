using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeSceneController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "LobbyScene";

    private void Start()
    {
        if (!TrainingRunState.IsRunActive || !TrainingRunState.PendingNodeId.HasValue)
        {
            ReturnToNextScene();
            return;
        }

        if (!TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode) ||
            pendingNode.nodeType != TrainingNodeType.Escape)
        {
            Debug.LogWarning("[EscapeSceneController] Pending node is not an Escape node. Returning.");
            ReturnToNextScene();
        }
    }

    public void OnClickEscape()
    {
        TrainingRunDeckPersistence.TrySaveRunDeckAsPermanentDeck(
            TrainingBattleManager.buildingDeck,
            SelectedButtonControl.selectedCharacterList,
            TrainingNodeType.Escape);

        TrainingRunState.CompletePendingNode(true);
        TrainingBattleManager.buildingDeck = null;
        ReturnToNextScene();
    }

    public void OnClickCancel()
    {
        ReturnToMap();
    }

    private void ReturnToMap()
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
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        ReturnToMapFallback();
    }

    private void ReturnToMapFallback()
    {
        if (!string.IsNullOrEmpty(TrainingRunState.MapSceneName))
        {
            SceneManager.LoadScene(TrainingRunState.MapSceneName);
        }
    }
}
