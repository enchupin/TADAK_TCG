using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Button healButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private int healAmount = 20;

    private bool healed;

    private void Start()
    {
        if (!TrainingRunState.IsRunActive || !TrainingRunState.PendingNodeId.HasValue)
        {
            ReturnToMap();
            return;
        }

        if (!TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode) ||
            pendingNode.nodeType != TrainingNodeType.Rest)
        {
            Debug.LogWarning("[RestSceneController] Pending node is not a Rest node. Returning to map.");
            ReturnToMap();
            return;
        }

        healed = false;

        if (healButton != null)
        {
            healButton.interactable = true;
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;
        }

        RefreshHpText();
    }

    public void OnClickHeal()
    {
        if (healed)
            return;

        if (!TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            Debug.LogWarning("[RestSceneController] No player HP state in TrainingRunState.");
            return;
        }

        int nextHp = Mathf.Clamp(hp + healAmount, 0, maxHp);
        TrainingRunState.SetPlayerHealthState(nextHp, maxHp);

        healed = true;

        if (healButton != null)
        {
            healButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.interactable = true;
        }

        RefreshHpText();
    }

    public void OnClickNext()
    {
        if (!healed)
            return;

        TrainingRunState.CompletePendingNode(true);
        ReturnToMap();
    }

    private void RefreshHpText()
    {
        if (hpText == null)
            return;

        if (TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            hpText.text = $"HP {hp} / {maxHp}";
            return;
        }

        hpText.text = "HP ? / ?";
    }

    private void ReturnToMap()
    {
        if (!string.IsNullOrEmpty(TrainingRunState.MapSceneName))
        {
            SceneManager.LoadScene(TrainingRunState.MapSceneName);
        }
    }
}
