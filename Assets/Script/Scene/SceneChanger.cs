using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "CombatScene";

    [Header("Training Mode Options")]
    [UnityEngine.Serialization.FormerlySerializedAs("infiniteModeButton")]
    [SerializeField] private Button bossModeButton;
    [UnityEngine.Serialization.FormerlySerializedAs("infiniteModeSelectedColor")]
    [SerializeField] private Color bossModeSelectedColor = new Color(0.4f, 0.9f, 1f, 1f);

    private bool isBossModeSelected;
    private bool hasBossModeDefaultColors;
    private ColorBlock bossModeDefaultColors;

    private void Awake()
    {
        CaptureBossModeButtonColors();
        RefreshBossModeButton();
    }

    public void StartTrainingRun()
    {
        StartTrainingRun(isBossModeSelected);
    }

    public void StartBossTrainingRun()
    {
        SetBossModeSelected(true);
        StartTrainingRun(true);
    }

    public void ToggleBossMode()
    {
        SetBossModeSelected(!isBossModeSelected);
    }

    public void SetBossModeSelected(bool isSelected)
    {
        isBossModeSelected = isSelected;
        ResolveBossModeButtonFromEvent();
        CaptureBossModeButtonColors();
        RefreshBossModeButton();
    }

    private void StartTrainingRun(bool isBossMode)
    {
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        BossMode.SetMode(isBossMode);
        TrainingRunState.StartNewRun(
            trainingMapSceneName,
            trainingBattleSceneName);
        SceneManager.LoadScene(trainingMapSceneName);
    }

    private void ResolveBossModeButtonFromEvent()
    {
        if (bossModeButton != null || EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null)
        {
            bossModeButton = selectedObject.GetComponent<Button>();
        }
    }

    private void CaptureBossModeButtonColors()
    {
        if (hasBossModeDefaultColors || bossModeButton == null)
        {
            return;
        }

        bossModeDefaultColors = bossModeButton.colors;
        hasBossModeDefaultColors = true;
    }

    private void RefreshBossModeButton()
    {
        if (bossModeButton == null)
        {
            return;
        }

        if (hasBossModeDefaultColors)
        {
            bossModeButton.colors = bossModeDefaultColors;
        }

        Color targetColor = hasBossModeDefaultColors
            ? bossModeDefaultColors.normalColor
            : Color.white;

        if (isBossModeSelected)
        {
            ColorBlock colors = bossModeButton.colors;
            colors.normalColor = bossModeSelectedColor;
            colors.highlightedColor = bossModeSelectedColor;
            colors.selectedColor = bossModeSelectedColor;
            bossModeButton.colors = colors;
            targetColor = bossModeSelectedColor;
        }

        if (bossModeButton.targetGraphic != null)
        {
            bossModeButton.targetGraphic.color = targetColor;
        }
    }
}
