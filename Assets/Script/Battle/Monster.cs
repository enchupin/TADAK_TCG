using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 몬스터의 전투 관련 데이터를 관리하는 클래스
/// </summary>
public class Monster : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI defenseText;

    [Header("Stats")]
    public int hp;
    public int maxHP;
    public int defense;
    public int attackPower;
    public new string name;
    public List<Buff> currentBuffs = new();

    private void Awake()
    {
        // 몬스터 기본 초기화
        if (maxHP <= 0)
        {
            maxHP = 100;
            hp = 100;
            name = "Dummy Monster";
            attackPower = 10;
            defense = 0;
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"HP : {hp}/{maxHP}";
        
        if (defenseText != null)
            defenseText.text = defense > 0 ? $"🛡 {defense}" : "";
    }

    /// <summary>
    /// 플레이어를 공격합니다.
    /// </summary>
    public void EnemyTurn(PlayerData target)
    {
        // 간단한 AI: 랜덤 데미지 (나중에 패턴 추가 가능)
        int damage = Random.Range(attackPower - 2, attackPower + 3); // 공격력 오차 범위 적용
        
        Debug.Log($"[적] {name} 공격! {damage} 데미지!");
        target.TakeDamage(damage);
    }

    /// <summary>
    /// 데미지를 받습니다. (방어력 적용)
    /// </summary>
    /// <returns>실제로 입힌 데미지</returns>
    public int TakeDamage(int amount, int playerStrength = 0)
    {
        int finalDamage = amount + playerStrength; // 플레이어 힘 버프 적용
        int damageAfterDefense = Mathf.Max(0, finalDamage - defense);

        hp -= damageAfterDefense;
        defense = Mathf.Max(0, defense - finalDamage);

        Debug.Log($"{name}이(가) {damageAfterDefense} 데미지를 받았습니다! (HP: {hp}/{maxHP})");
        UpdateUI();
        return damageAfterDefense;  // 실제 입힌 데미지 반환
    }

    /// <summary>
    /// 방어력을 추가합니다.
    /// </summary>
    public void AddDefense(int amount)
    {
        defense += amount;
        Debug.Log($"{name}의 방어력 +{amount} (현재: {defense})");
        UpdateUI();
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
            Debug.Log($"[적] 버프 중첩: {data.name} (+{amount}) -> {existingBuff.stack}");
        }
        else
        {
            Buff newBuff = new Buff(data, amount, 0); 
            currentBuffs.Add(newBuff);
            Debug.Log($"[적] 버프 획득: {data.name} ({amount})");
        }
    }

    /// <summary>
    /// 턴 시작 시 초기화
    /// </summary>
    public void OnTurnStart()
    {
        // 몬스터의 턴 시작 처리 (필요시 구현)
    }

    /// <summary>
    /// 턴 종료 시 처리
    /// </summary>
    public void OnTurnEnd()
    {
        // 몬스터의 턴 종료 처리 (필요시 구현)
    }

    /// <summary>
    /// 몬스터가 죽었는지 확인
    /// </summary>
    public bool IsDead()
    {
        return hp <= 0;
    }

    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(int amount)
    {
        int healAmount = Mathf.Min(amount, maxHP - hp);
        hp += healAmount;
        Debug.Log($"{name}의 체력 +{healAmount} 회복 (현재: {hp}/{maxHP})");
        UpdateUI();
    }
}
