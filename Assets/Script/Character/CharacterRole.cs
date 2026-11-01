/// <summary>
/// 캐릭터 역할 (서포터 · 딜러 · 탱커)
/// 캐릭터 ID 백의 자리로 판정: 1xx 서포터, 2xx 딜러, 3xx 탱커
/// </summary>
public enum CharacterRole
{
    None,
    Supporter,
    Dealer,
    Tank
}

public static class CharacterRoles
{
    public static CharacterRole FromCharacterId(int characterId)
    {
        if (characterId < 100 || characterId > 999)
        {
            return CharacterRole.None;
        }

        return (characterId / 100) switch
        {
            1 => CharacterRole.Supporter,
            2 => CharacterRole.Dealer,
            3 => CharacterRole.Tank,
            _ => CharacterRole.None
        };
    }
}
