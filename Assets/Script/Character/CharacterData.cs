using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "TCG/Character Data")]
public class CharacterData : ScriptableObject
{
    public int characterId;
    public string characterName;
    public int maxHp;
    public CharacterIdentityDefinition identity = new CharacterIdentityDefinition();
}
