using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DictionaryCardLayout : MonoBehaviour
{
    public const int RequiredCardCount = 7;

    [Header("씬 카드 배치 미리보기")]
    [Tooltip("카드 순서대로 연결하며 각 임시 카드의 RectTransform을 씬에서 조정")]
    [SerializeField] private Image[] debugCards;

    private struct CardPosition
    {
        public bool isValid;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector3 anchoredPosition;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private CardPosition[] cardPositions;

    public bool HasCardPositions
    {
        get
        {
            CaptureCardPositions();
            return cardPositions != null && cardPositions.Length > 0;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= CaptureSceneCardPositions;
        SceneManager.sceneLoaded += CaptureSceneCardPositions;
    }

    private static void CaptureSceneCardPositions(Scene scene, LoadSceneMode mode)
    {
        // 비활성 패널의 임시 카드도 씬을 불러올 때 함께 제거
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (DictionaryCardLayout layout in root.GetComponentsInChildren<DictionaryCardLayout>(true))
            {
                layout.CaptureCardPositions();
            }
        }
    }

    private void Awake()
    {
        CaptureCardPositions();
    }

    private void CaptureCardPositions()
    {
        if (!Application.isPlaying || cardPositions != null)
        {
            return;
        }

        if (debugCards == null || debugCards.Length != RequiredCardCount)
        {
            throw new System.InvalidOperationException($"[DictionaryCardLayout] 임시 카드는 정확히 {RequiredCardCount}개여야 합니다: {name}");
        }

        cardPositions = new CardPosition[RequiredCardCount];
        for (int i = 0; i < cardPositions.Length; i++)
        {
            Image preview = debugCards[i];
            if (preview == null || preview.transform.parent != transform)
            {
                Debug.LogError("[DictionaryCardLayout] 배치용 카드를 같은 패널의 자식으로 연결해야 합니다", this);
                continue;
            }

            RectTransform slot = preview.rectTransform;
            cardPositions[i] = new CardPosition
            {
                isValid = true,
                anchorMin = slot.anchorMin,
                anchorMax = slot.anchorMax,
                pivot = slot.pivot,
                sizeDelta = slot.sizeDelta,
                anchoredPosition = slot.anchoredPosition3D,
                rotation = slot.localRotation,
                scale = slot.localScale
            };

            preview.gameObject.SetActive(false);
            Destroy(preview.gameObject);
        }

        debugCards = null;
    }

    public bool PlaceCard(RectTransform card, int index)
    {
        if (card == null || !HasCardPositions || index < 0 || index >= RequiredCardCount)
        {
            Debug.LogError("[DictionaryCardLayout] 카드 배치 위치가 연결되지 않았습니다", this);
            return false;
        }

        CardPosition slot = cardPositions[index];
        if (!slot.isValid)
        {
            Debug.LogError("[DictionaryCardLayout] 카드 배치 위치가 연결되지 않았습니다", this);
            return false;
        }

        card.SetParent(transform, false);
        card.anchorMin = slot.anchorMin;
        card.anchorMax = slot.anchorMax;
        card.pivot = slot.pivot;
        card.sizeDelta = slot.sizeDelta;
        card.anchoredPosition3D = slot.anchoredPosition;
        card.localRotation = slot.rotation;
        card.localScale = slot.scale;

        return true;
    }
}
