using System;
using UnityEngine;
using UnityEngine.UI;

// R-46 네임드 전투 승리 보상 선택 창: 카드 복제 또는 카드 제거
// 씬 배치와 버튼 연결은 인스펙터에서 지정 (root · duplicateButton · removeButton)
public class NamedRewardChoicePanel : MonoBehaviour
{
    public enum Choice
    {
        Duplicate,
        Remove
    }

    [SerializeField] private GameObject root;
    [SerializeField] private Button duplicateButton;
    [SerializeField] private Button removeButton;

    private Action<Choice> onChosen;

    private void Awake()
    {
        if (duplicateButton != null)
        {
            duplicateButton.onClick.AddListener(() => Choose(Choice.Duplicate));
        }

        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => Choose(Choice.Remove));
        }

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    // 창을 열 수 없으면 false (호출 측에서 기본 보상 처리)
    public bool Show(Action<Choice> callback)
    {
        if (root == null || duplicateButton == null || removeButton == null || callback == null)
        {
            return false;
        }

        onChosen = callback;
        root.SetActive(true);
        return true;
    }

    private void Choose(Choice choice)
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        Action<Choice> callback = onChosen;
        onChosen = null;
        callback?.Invoke(choice);
    }
}
