using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 카드 선택 중 선택된 카드에 일어날 일을 안내 (손패 선택 줄 · 목록 선택 패널 공용 텍스트 하나)
/// 선택이 보이는 동안만 표시, 결과를 알 수 없는 선택이면 숨김
/// </summary>
public class SelectionOutcomeView : MonoBehaviour
{
    [Tooltip("결과 문구 텍스트 (비우면 같은 오브젝트의 TMP)")]
    [SerializeField] private TMP_Text outcomeText;

    private IReadOnlyList<CardSelectionOutcome> shownOutcomes;
    private string shownText = string.Empty;

    private void Awake()
    {
        if (outcomeText == null)
        {
            TryGetComponent(out outcomeText);
        }

        if (outcomeText != null)
        {
            outcomeText.raycastTarget = false;
            outcomeText.enabled = false;
        }
        else
        {
            Debug.LogWarning("[SelectionOutcomeView] 결과 문구 텍스트가 연결되지 않았습니다", this);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += ForceRebuild;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= ForceRebuild;
    }

    private void ForceRebuild()
    {
        shownOutcomes = null;
    }

    private void LateUpdate()
    {
        if (outcomeText == null)
        {
            return;
        }

        TrainingBattleManager manager = TrainingBattleManager.Instance;
        IReadOnlyList<CardSelectionOutcome> outcomes = manager != null && manager.IsCardSelectionVisible ? manager.SelectionOutcomes : null;
        if (!ReferenceEquals(outcomes, shownOutcomes))
        {
            shownOutcomes = outcomes;
            shownText = CardSelectionOutcomeText.Build(outcomes, key => GameText.Ui(key));
            outcomeText.text = shownText;
        }

        bool visible = !string.IsNullOrEmpty(shownText);
        if (outcomeText.enabled != visible)
        {
            outcomeText.enabled = visible;
        }
    }
}
