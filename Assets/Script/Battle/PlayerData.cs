using UnityEngine;

/// <summary>
/// 플레이어의 전투 관련 데이터를 관리하는 클래스
/// </summary>
public class PlayerData
{
    // 체력 관련
    public int hp;
    public int maxHP;

    // 방어력
    public int defense;

    // 에너지
    public int energy;
    public int maxEnergy;

    // 버프/디버프
    public int strength; // 힘 버프 (Legacy support for now)
    public System.Collections.Generic.List<Buff> currentBuffs = new System.Collections.Generic.List<Buff>();

    /// <summary>
    /// 방어력을 추가합니다.
    /// </summary>
    public void AddDefense(int amount)
    {
        defense += amount;
        Debug.Log($"방어력 +{amount} (현재: {defense})");
    }

    /// <summary>
    /// 힘 버프를 추가합니다.
    /// </summary>
    public void AddStrength(int amount)
    {
        strength += amount;
        Debug.Log($"힘 +{amount} (현재: {strength})");
    }

    /// <summary>
    /// 버프를 추가합니다.
    /// </summary>
    public void AddBuff(int buffId, int amount)
    {
        BuffData data = BuffManager.Instance.GetBuffData(buffId);
        if (data == null) return;

        Buff existingBuff = currentBuffs.Find(b => b.data.buffId == buffId);
        if (existingBuff != null)
        {
            existingBuff.stack += amount;
            Debug.Log($"버프 중첩: {data.name} (+{amount}) -> {existingBuff.stack}");
        }
        else
        {
            Buff newBuff = new Buff(data, amount, 0); // Duration logic TBD
            currentBuffs.Add(newBuff);
            Debug.Log($"버프 획득: {data.name} ({amount})");
        }
    }

    /// <summary>
    /// 에너지를 추가합니다.
    /// </summary>
    public void AddEnergy(int amount)
    {
        energy = Mathf.Min(energy + amount, maxEnergy);
        Debug.Log($"에너지 +{amount} (현재: {energy}/{maxEnergy})");
    }

    /// <summary>
    /// 에너지를 사용합니다.
    /// </summary>
    public bool UseEnergy(int amount)
    {
        if (energy >= amount)
        {
            energy -= amount;
            return true;
        }
        Debug.LogWarning($"에너지가 부족합니다! (필요: {amount}, 현재: {energy})");
        return false;
    }

    /// <summary>
    /// 데미지를 받습니다. (방어력 적용)
    /// </summary>
    public void TakeDamage(int amount)
    {
        int damageAfterDefense = Mathf.Max(0, amount - defense);
        hp -= damageAfterDefense;
        defense = Mathf.Max(0, defense - amount);

        Debug.Log($"플레이어가 {damageAfterDefense} 데미지를 받았습니다! (HP: {hp}/{maxHP})");
    }

    /// <summary>
    /// 체력을 회복합니다.
    /// </summary>
    public void Heal(int amount)
    {
        int healAmount = Mathf.Min(amount, maxHP - hp);
        hp += healAmount;
        Debug.Log($"체력 +{healAmount} 회복 (현재: {hp}/{maxHP})");
    }

    /// <summary>
    /// 턴 시작 시 초기화 (방어력 리셋, 에너지 회복 등)
    /// </summary>
    public void OnTurnStart()
    {
        defense = 0; // 방어력 리셋
        energy = maxEnergy; // 에너지 회복
        Debug.Log("턴 시작: 방어력 리셋, 에너지 회복");
        
        // Buff trigger processing would go here
    }

    /// <summary>
    /// 턴 종료 시 처리
    /// </summary>
    public void OnTurnEnd()
    {
        // 턴 종료 시 필요한 처리 (예: 턴 지속 버프 감소 등)
    }

    /// <summary>
    /// 플레이어가 죽었는지 확인
    /// </summary>
    public bool IsDead()
    {
        return hp <= 0;
    }
}
