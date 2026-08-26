using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    [Header("Training Run Flow")]
    [SerializeField] private string trainingMapSceneName = "TrainingMapScene";
    [SerializeField] private string trainingBattleSceneName = "TrainingScene";

    [Header("Training Mode Options")]
    [SerializeField] private Button infiniteModeButton;
    [SerializeField] private Color infiniteModeSelectedColor = new Color(0.4f, 0.9f, 1f, 1f);

    private bool isInfiniteModeSelected;
    private bool hasInfiniteModeDefaultColors;
    private ColorBlock infiniteModeDefaultColors;

    private void Awake()
    {
        CaptureInfiniteModeButtonColors();
        RefreshInfiniteModeButton();
    }

    public void StartTrainingRun()
    {
        StartTrainingRun(isInfiniteModeSelected);
    }

    public void StartInfiniteTrainingRun()
    {
        SetInfiniteModeSelected(true);
        StartTrainingRun(true);
    }

    public void ToggleInfiniteMode()
    {
        SetInfiniteModeSelected(!isInfiniteModeSelected);
    }

    public void SetInfiniteModeSelected(bool isSelected)
    {
        isInfiniteModeSelected = isSelected;
        ResolveInfiniteModeButtonFromEvent();
        CaptureInfiniteModeButtonColors();
        RefreshInfiniteModeButton();
    }

    private void StartTrainingRun(bool isInfiniteMode)
    {
        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        InfiniteMode.SetMode(isInfiniteMode);
        TrainingRunState.StartNewRun(
            trainingMapSceneName,
            trainingBattleSceneName);
        SceneManager.LoadScene(trainingMapSceneName);
    }

    private void ResolveInfiniteModeButtonFromEvent()
    {
        if (infiniteModeButton != null || EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null)
        {
            infiniteModeButton = selectedObject.GetComponent<Button>();
        }
    }

    private void CaptureInfiniteModeButtonColors()
    {
        if (hasInfiniteModeDefaultColors || infiniteModeButton == null)
        {
            return;
        }

        infiniteModeDefaultColors = infiniteModeButton.colors;
        hasInfiniteModeDefaultColors = true;
    }

    private void RefreshInfiniteModeButton()
    {
        if (infiniteModeButton == null)
        {
            return;
        }

        if (hasInfiniteModeDefaultColors)
        {
            infiniteModeButton.colors = infiniteModeDefaultColors;
        }

        Color targetColor = hasInfiniteModeDefaultColors
            ? infiniteModeDefaultColors.normalColor
            : Color.white;

        if (isInfiniteModeSelected)
        {
            ColorBlock colors = infiniteModeButton.colors;
            colors.normalColor = infiniteModeSelectedColor;
            colors.highlightedColor = infiniteModeSelectedColor;
            colors.selectedColor = infiniteModeSelectedColor;
            infiniteModeButton.colors = colors;
            targetColor = infiniteModeSelectedColor;
        }

        if (infiniteModeButton.targetGraphic != null)
        {
            infiniteModeButton.targetGraphic.color = targetColor;
        }
    }
}
