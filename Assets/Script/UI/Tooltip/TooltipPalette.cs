using UnityEngine;

/// <summary>
/// 인스펙터에서 조정하는 툴팁 강조 색상
/// </summary>
[System.Serializable]
public class TooltipPalette
{
    [Tooltip("이로운 효과 · 패시브 이름 (라피스)")]
    public Color beneficial = new Color32(0x26, 0x61, 0x9C, 0xFF);

    [Tooltip("해로운 효과 이름 (스칼렛)")]
    public Color harmful = new Color32(0xFF, 0x24, 0x00, 0xFF);

    [Tooltip("의도 이름 (올리브)")]
    public Color intent = new Color32(0x80, 0x80, 0x00, 0xFF);

    [Tooltip("수치 (아프리콧)")]
    public Color value = new Color32(0xFB, 0xCE, 0xB1, 0xFF);

    public TooltipColors ToColors()
    {
        return new TooltipColors(
            ColorUtility.ToHtmlStringRGBA(beneficial),
            ColorUtility.ToHtmlStringRGBA(harmful),
            ColorUtility.ToHtmlStringRGBA(intent),
            ColorUtility.ToHtmlStringRGBA(value));
    }
}
