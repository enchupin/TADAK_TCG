using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 랭킹 모드 씬에서 캐릭터 선택 버튼 관리
/// 클릭 시 덱 선택 버튼들을 생성하고 뒤의 버튼들을 밀어냄
/// </summary>
public class CharacterSelectButton_RankingScene : MonoBehaviour
{
    [Header("캐릭터 설정")]
    [SerializeField] private Character character; // 이 버튼이 담당하는 캐릭터

    [Header("덱 선택 버튼 프리팹")]
    [SerializeField] private GameObject deckSelectButtonPrefab; // 덱 선택 버튼 프리팹

    [Header("레이아웃 설정")]
    [SerializeField] private Transform scrollViewContent; // ScrollView의 Content (부모 Transform)

    private List<GameObject> deckSelectButtons = new List<GameObject>(); // 생성된 덱 선택 버튼들
    private bool isExpanded = false; // 덱 선택 버튼들이 펼쳐진 상태인지 확인

    private void Awake()
    {
        // ScrollView Content에 Horizontal Layout Group이 있는지 확인하고 없으면 추가
        SetupLayoutGroup();
    }

    /// <summary>
    /// Horizontal Layout Group 설정
    /// </summary>
    private void SetupLayoutGroup()
    {
        if (scrollViewContent == null) return;

        HorizontalLayoutGroup layoutGroup = scrollViewContent.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = scrollViewContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.spacing = 10f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            Debug.Log("[CharacterSelectButton_RankingScene] Horizontal Layout Group 추가됨");
        }

        // Content Size Fitter도 추가 (스크롤을 위해 필요)
        ContentSizeFitter sizeFitter = scrollViewContent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = scrollViewContent.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            Debug.Log("[CharacterSelectButton_RankingScene] Content Size Fitter 추가됨");
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출
    /// </summary>
    public void OnButtonClick()
    {
        if (isExpanded)
        {
            // 이미 펼쳐져 있으면 덱 선택 버튼들 제거
            CollapseDeckButtons();
        }
        else
        {
            // 펼쳐져 있지 않으면 덱 선택 버튼들 생성
            ExpandDeckButtons();
        }
    }

    /// <summary>
    /// 덱 선택 버튼들 생성 (캐릭터 버튼 바로 우측에)
    /// </summary>
    private void ExpandDeckButtons()
    {
        if (deckSelectButtonPrefab == null)
        {
            Debug.LogError("[CharacterSelectButton_RankingScene] 덱 선택 버튼 프리팹이 설정되지 않았습니다!");
            return;
        }

        if (scrollViewContent == null)
        {
            Debug.LogError("[CharacterSelectButton_RankingScene] ScrollView Content가 설정되지 않았습니다!");
            return;
        }

        // 현재 버튼의 형제 인덱스 확인
        int currentIndex = transform.GetSiblingIndex();

        // 덱 선택 버튼 3개 생성
        for (int i = 0; i < 3; i++)
        {
            // 프리팹 인스턴스화
            GameObject deckButton = Instantiate(deckSelectButtonPrefab, scrollViewContent);

            // 버튼을 캐릭터 버튼 바로 다음에 순서대로 배치
            deckButton.transform.SetSiblingIndex(currentIndex + 1 + i);

            // 생성된 버튼 리스트에 추가
            deckSelectButtons.Add(deckButton);

            Debug.Log($"[CharacterSelectButton_RankingScene] 덱 버튼 생성 (인덱스: {currentIndex + 1 + i})");
        }

        isExpanded = true;

        // 레이아웃 강제 리빌드
        ForceRebuildLayout();
    }

    /// <summary>
    /// 생성된 덱 선택 버튼들 제거
    /// </summary>
    private void CollapseDeckButtons()
    {
        foreach (GameObject deckButton in deckSelectButtons)
        {
            if (deckButton != null)
            {
                Destroy(deckButton);
            }
        }

        deckSelectButtons.Clear();
        isExpanded = false;

        // 레이아웃 강제 리빌드
        ForceRebuildLayout();
    }

    /// <summary>
    /// 레이아웃 강제 리빌드
    /// </summary>
    private void ForceRebuildLayout()
    {
        if (scrollViewContent == null) return;

        // Canvas 업데이트 강제
        Canvas.ForceUpdateCanvases();

        // Layout 리빌드
        RectTransform rectTransform = scrollViewContent.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 덱 선택 버튼들도 함께 제거
    /// </summary>
    private void OnDestroy()
    {
        CollapseDeckButtons();
    }
}

