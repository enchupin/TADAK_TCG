using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 이미지 위 과녁 표시 (몬스터 선택 · 이후 카드 드래그 대상 지정에서도 사용)
/// 이미지가 없으면 임시 붉은 원으로 표시
/// </summary>
public static class MonsterTargetMark
{
    // 과녁 이미지 위치 (Resources 기준, 확장자 제외)
    public const string SpritePath = "Image/BattleSceneUI/TargetMark";
    private const string MarkObjectName = "TargetMark";
    private const int FallbackTextureSize = 128;
    // 몬스터 이미지 짧은 변 대비 과녁 크기
    private const float SizeRatio = 0.8f;

    private static Sprite cachedSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedSprite = null;
    }

    public static void SetVisible(Image monsterImage, bool visible)
    {
        if (monsterImage == null)
        {
            return;
        }

        Transform existing = monsterImage.transform.Find(MarkObjectName);
        if (!visible)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
            return;
        }

        Image mark = existing != null ? existing.GetComponent<Image>() : CreateMark(monsterImage.transform);
        if (mark == null)
        {
            return;
        }

        RectTransform imageRect = monsterImage.rectTransform;
        float size = Mathf.Min(imageRect.rect.width, imageRect.rect.height) * SizeRatio;
        mark.rectTransform.sizeDelta = new Vector2(size, size);
        mark.transform.SetAsLastSibling();
        mark.gameObject.SetActive(true);
    }

    private static Image CreateMark(Transform parent)
    {
        GameObject markObject = new GameObject(MarkObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markObject.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)markObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = markObject.GetComponent<Image>();
        image.sprite = ResolveSprite();
        image.preserveAspect = true;
        // 몬스터 클릭을 가리지 않음
        image.raycastTarget = false;
        return image;
    }

    private static Sprite ResolveSprite()
    {
        if (cachedSprite != null)
        {
            return cachedSprite;
        }

        cachedSprite = Resources.Load<Sprite>(SpritePath);
        if (cachedSprite == null)
        {
            cachedSprite = CreateFallbackRing();
        }
        return cachedSprite;
    }

    // 과녁 이미지가 제공되기 전 임시 표시: 붉은 고리
    private static Sprite CreateFallbackRing()
    {
        int size = FallbackTextureSize;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color ring = new Color(0.9f, 0.1f, 0.1f, 0.9f);
        Color clear = new Color(0f, 0f, 0f, 0f);
        float center = (size - 1) * 0.5f;
        float outer = size * 0.5f;
        float inner = outer * 0.8f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= outer && distance >= inner ? ring : clear);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
