using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 단일 대상 카드를 끌어 몬스터 인식 범위에 들어오면 그 몬스터 위에 과녁 표시
/// 인식 대상은 손패 설명 미리보기 대상(TrainingBattleManager)과 같음, 대상이 없으면 숨김
/// </summary>
// 과녁 이미지는 전투 영역에서 몬스터보다 뒤 순서(위에 그려짐)에 두고 이 컴포넌트에 연결
public class TargetReticleView : MonoBehaviour
{
    [Tooltip("과녁 이미지 (Image/BattleSceneUI/과녁), 클릭 판정은 코드에서 끔")]
    [SerializeField] private Image reticleImage;

    [Header("크기")]
    [Tooltip("켜면 몬스터 그림 크기에 비례, 끄면 고정 크기")]
    [SerializeField] private bool scaleWithMonster;
    [SerializeField] private Vector2 fixedSize = new Vector2(160f, 160f);
    [Tooltip("비례 크기: 몬스터 그림 짧은 변 대비 과녁 크기")]
    [SerializeField] private float monsterSizeRatio = 0.6f;

    [Header("위치")]
    [Tooltip("몬스터 그림 영역 안 기준점 (0,0 좌하단 ~ 1,1 우상단)")]
    [SerializeField] private Vector2 anchorInMonster = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 offset;

    private readonly Vector3[] corners = new Vector3[4];
    private RectTransform reticleRect;

    private void Awake()
    {
        if (reticleImage == null)
        {
            TryGetComponent(out reticleImage);
        }

        if (reticleImage != null)
        {
            reticleImage.raycastTarget = false;
            reticleImage.preserveAspect = true;
            reticleRect = reticleImage.rectTransform;
        }
        else
        {
            Debug.LogWarning("[TargetReticleView] 과녁 이미지가 연결되지 않았습니다", this);
        }

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (reticleRect == null)
        {
            return;
        }

        TrainingBattleManager manager = TrainingBattleManager.Instance;
        Monster target = manager != null ? manager.GetDescriptionTarget(previewOnly: true) : null;
        if (target == null || target.IsDead() || !target.TryGetImageContentWorldCorners(corners))
        {
            SetVisible(false);
            return;
        }

        Place();
        SetVisible(true);
    }

    // 몬스터 그림 영역을 과녁 부모 좌표로 바꿔 배치 (몬스터가 움직여도 매 프레임 따라감)
    private void Place()
    {
        RectTransform parent = reticleRect.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = parent.InverseTransformPoint(corners[2]);
        Vector2 size = max - min;
        Vector2 point = min + Vector2.Scale(size, anchorInMonster) + offset;

        Vector2 reticleSize = scaleWithMonster
            ? Vector2.one * Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)) * Mathf.Max(0f, monsterSizeRatio)
            : fixedSize;

        reticleRect.anchorMin = new Vector2(0.5f, 0.5f);
        reticleRect.anchorMax = new Vector2(0.5f, 0.5f);
        reticleRect.pivot = new Vector2(0.5f, 0.5f);
        reticleRect.sizeDelta = reticleSize;
        reticleRect.anchoredPosition = point - parent.rect.center;
    }

    private void SetVisible(bool visible)
    {
        if (reticleImage != null && reticleImage.enabled != visible)
        {
            reticleImage.enabled = visible;
        }
    }
}
