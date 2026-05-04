using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BuffUI : MonoBehaviour
{
    private const int DefaultBuffIconId = 1000;

    [Header("버프 아이콘 설정")]
    [SerializeField] private RectTransform buffRoot;
    [SerializeField] private Vector2 firstIconPosition = new Vector2(30f, -30f);
    [SerializeField] private Vector2 iconSize = new Vector2(30f, 30f);
    [SerializeField] private float iconSpacing = 60f;
    [SerializeField] private int iconsPerRow = 7;

    private readonly List<Image> createdIcons = new();
    private readonly Dictionary<int, Sprite> iconCache = new();
    private string currentBuffSignature;

    private void OnEnable()
    {
        currentBuffSignature = null;
        RefreshIfNeeded();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshIfNeeded();
    }

    // Update is called once per frame
    void Update()
    {
        RefreshIfNeeded();
    }

    private void OnDestroy()
    {
        ClearIconObjects();
        iconCache.Clear();
    }

    public void Refresh()
    {
        List<int> activeBuffIds = CollectActiveBuffIds();
        RebuildIcons(activeBuffIds);
        currentBuffSignature = BuildBuffSignature(activeBuffIds);
    }

    private void RefreshIfNeeded()
    {
        List<int> activeBuffIds = CollectActiveBuffIds();
        string nextBuffSignature = BuildBuffSignature(activeBuffIds);
        if (currentBuffSignature == nextBuffSignature)
        {
            return;
        }

        RebuildIcons(activeBuffIds);
        currentBuffSignature = nextBuffSignature;
    }

    private List<int> CollectActiveBuffIds()
    {
        List<int> activeBuffIds = new();
        PlayerData player = PlayerData.Instance;
        if (player == null || player.currentBuffs == null)
        {
            return activeBuffIds;
        }

        foreach (Buff buff in player.currentBuffs)
        {
            int buffId = GetBuffId(buff);
            if (buffId > 0)
            {
                activeBuffIds.Add(buffId);
            }
        }

        return activeBuffIds;
    }

    private static int GetBuffId(Buff buff)
    {
        if (buff == null || buff.stack <= 0 || buff.data == null)
        {
            return 0;
        }

        return buff.data.buffId;
    }

    private static string BuildBuffSignature(List<int> activeBuffIds)
    {
        if (activeBuffIds == null || activeBuffIds.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach (int buffId in activeBuffIds)
        {
            builder.Append(buffId);
            builder.Append('|');
        }

        return builder.ToString();
    }

    private void RebuildIcons(List<int> activeBuffIds)
    {
        EnsureBuffRoot();
        ClearIconObjects();

        if (buffRoot == null || activeBuffIds == null)
        {
            return;
        }

        int safeIconsPerRow = Mathf.Max(1, iconsPerRow);
        for (int i = 0; i < activeBuffIds.Count; i++)
        {
            Image iconImage = CreateBuffImage(i, safeIconsPerRow);
            if (iconImage == null)
            {
                continue;
            }

            createdIcons.Add(iconImage);
            SetIconSprite(activeBuffIds[i], iconImage);
        }
    }

    private void EnsureBuffRoot()
    {
        if (buffRoot != null)
        {
            return;
        }

        Transform foundBuffRoot = transform.Find("Buff");
        if (foundBuffRoot is RectTransform foundRectTransform)
        {
            buffRoot = foundRectTransform;
            return;
        }

        buffRoot = transform as RectTransform;
    }

    private Image CreateBuffImage(int index, int safeIconsPerRow)
    {
        GameObject iconObject = new GameObject($"BuffImage ({index + 1})", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(buffRoot, false);

        RectTransform rectTransform = iconObject.transform as RectTransform;
        if (rectTransform == null)
        {
            Destroy(iconObject);
            return null;
        }

        int column = index % safeIconsPerRow;
        int row = index / safeIconsPerRow;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = iconSize;
        rectTransform.anchoredPosition = firstIconPosition + new Vector2(column * iconSpacing, -row * iconSpacing);
        rectTransform.localScale = Vector3.one;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.enabled = false;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        return iconImage;
    }

    private void SetIconSprite(int buffId, Image iconImage)
    {
        if (iconImage == null || buffId <= 0)
        {
            return;
        }

        if (iconCache.TryGetValue(buffId, out Sprite cachedSprite) && cachedSprite != null)
        {
            ApplySprite(iconImage, cachedSprite);
            return;
        }

        Sprite loadedSprite = Resources.Load<Sprite>(BuildIconResourcePath(buffId));
        if (loadedSprite == null)
        {
            loadedSprite = Resources.Load<Sprite>(BuildIconResourcePath(DefaultBuffIconId));
            Debug.LogWarning($"버프 아이콘을 찾을 수 없습니다: {buffId}");
        }

        if (loadedSprite == null)
        {
            Debug.LogError($"버프 아이콘을 찾을 수 없습니다: {buffId}");
            return;
        }

        iconCache[buffId] = loadedSprite;
        ApplySprite(iconImage, loadedSprite);
    }

    private string BuildIconResourcePath(int buffId)
    {
        return $"Image/BuffIcon/{buffId}";
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = true;
        image.color = Color.white;
    }

    private void ClearIconObjects()
    {
        foreach (Image image in createdIcons)
        {
            if (image != null)
            {
                Destroy(image.gameObject);
            }
        }

        createdIcons.Clear();
    }
}
