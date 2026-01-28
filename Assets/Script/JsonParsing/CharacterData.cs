using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 데이터를 저장하는 ScriptableObject
/// JSON에서 변환되어 .asset 파일로 저장됩니다.
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "TCG/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public int characterId;
    public string characterName;
    public int maxHp;
    public int cost;
    public string characterColor;
    
    [Header("Identity (고유 스킬)")]
    public IdentitySkillData identity;
    
    [Header("Addressables 주소")]
    public string artworkAddress;
    public string effectAddress;
    public string soundAddress;
    
    [Header("시작 덱")]
    public List<int> startDeckCardIds = new List<int>();
}

/// <summary>
/// 캐릭터 고유 스킬 데이터
/// </summary>
[System.Serializable]
public class IdentitySkillData
{
    public int cost;
    public string description;
    public List<IdentityEffectData> effects = new List<IdentityEffectData>();
}

/// <summary>
/// Identity 효과 데이터
/// </summary>
[System.Serializable]
public class IdentityEffectData
{
    public IdentityEffectType type;
    
    [Header("공통")]
    public int amount;
    public IdentityTargetType target;
    
    [Header("Buff")]
    public string buffType;
    
    [Header("GenerateCard")]
    public int cardId;
}

/// <summary>
/// Identity 효과 타입
/// </summary>
public enum IdentityEffectType
{
    Buff,           // 버프 부여
    Draw,           // 카드 드로우
    GenerateCard,   // 특정 카드 생성
    Damage,         // 피해
    Heal            // 회복
}

/// <summary>
/// Identity 타겟 타입
/// </summary>
public enum IdentityTargetType
{
    Self,           // 자신
    AllEnemies,     // 모든 적
    RandomEnemy,    // 랜덤 적
    Hand            // 손패
}
