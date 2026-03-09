using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 CharacterData ScriptableObject를 담는 컬렉션
/// CardCollection과 동일한 구조
/// </summary>
[CreateAssetMenu(fileName = "CharacterCollection", menuName = "TCG/Character Collection")]
public class CharacterCollection : ScriptableObject
{
    [Header("모든 캐릭터 데이터")]
    public List<CharacterData> allCharacters = new List<CharacterData>();
    
    /// <summary>
    /// ID로 캐릭터 찾기
    /// </summary>
    public CharacterData GetCharacterById(int characterId)
    {
        return allCharacters.Find(c => c.characterId == characterId);
    }
    
    /// <summary>
    /// 이름으로 캐릭터 찾기
    /// </summary>
    public CharacterData GetCharacterByName(string name)
    {
        return allCharacters.Find(c => c.characterName == name);
    }
}
