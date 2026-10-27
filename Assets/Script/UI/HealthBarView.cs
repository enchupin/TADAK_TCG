using TMPro;
using UnityEngine;

/// <summary>
/// 체력 바 위의 체력 수치와 보호막 표시를 갱신
/// 슬라이더 값과 Fill 색은 기존처럼 소유자(Monster · BattleUI)가 처리
/// </summary>
[DisallowMultipleComponent]
public class HealthBarView : MonoBehaviour
{
    [Header("체력 수치")]
    [Tooltip("체력 바 중앙에 현재/최대 체력을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI valueText;

    [Header("보호막")]
    [Tooltip("체력 바 좌측 끝의 방패 아이콘과 수치를 묶은 오브젝트, 보호막이 0이면 숨김")]
    [SerializeField] private GameObject barrierRoot;
    [Tooltip("방패 아이콘 위에 보호막 수치를 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI barrierText;

    public void Refresh(int hp, int maxHp, int barrier)
    {
        if (valueText != null)
        {
            valueText.text = FormatValue(hp, maxHp);
        }

        bool hasBarrier = barrier > 0;
        if (barrierRoot != null && barrierRoot.activeSelf != hasBarrier)
        {
            barrierRoot.SetActive(hasBarrier);
        }

        if (barrierText != null)
        {
            barrierText.text = hasBarrier ? barrier.ToString() : string.Empty;
        }
    }

    public static string FormatValue(int hp, int maxHp)
    {
        return $"{Mathf.Max(0, hp)}/{Mathf.Max(0, maxHp)}";
    }
}
