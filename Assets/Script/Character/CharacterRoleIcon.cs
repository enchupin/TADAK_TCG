using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 버튼의 역할 아이콘 (서포터 · 딜러 · 탱커)
/// 버튼 프리팹 안 아이콘 이미지에 붙이고 스프라이트 3종을 인스펙터에서 연결
/// </summary>
public class CharacterRoleIcon : MonoBehaviour
{
    [Tooltip("아이콘을 표시할 이미지 (비우면 같은 오브젝트의 Image)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite supporterIcon;
    [SerializeField] private Sprite dealerIcon;
    [SerializeField] private Sprite tankIcon;

    public void Bind(int characterId)
    {
        if (iconImage == null)
        {
            TryGetComponent(out iconImage);
        }

        if (iconImage == null)
        {
            Debug.LogWarning("[CharacterRoleIcon] 아이콘 이미지가 연결되지 않았습니다", this);
            return;
        }

        Sprite sprite = ResolveSprite(CharacterRoles.FromCharacterId(characterId));
        iconImage.sprite = sprite;
        // 역할을 알 수 없거나 스프라이트가 비어 있으면 숨김
        iconImage.enabled = sprite != null;
    }

    private Sprite ResolveSprite(CharacterRole role)
    {
        return role switch
        {
            CharacterRole.Supporter => supporterIcon,
            CharacterRole.Dealer => dealerIcon,
            CharacterRole.Tank => tankIcon,
            _ => null
        };
    }
}
