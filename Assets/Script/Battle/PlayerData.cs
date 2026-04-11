using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 전투 관련 데이터를 관리하는 클래스
/// </summary>
public enum WuppiModeState
{
    Normal,
    Guard,
    Attack
}

public class PlayerData : MonoBehaviour
{
    /// <summary>싱글톤 인스턴스 (TrainingBattleManager.InitializeBattle()에서 생성)</summary>
    public static PlayerData Instance { get; private set; }
    
    /// <summary>새 인스턴스를 생성하고 싱글톤으로 등록</summary>
    public static PlayerData Create() {
        if (Instance == null)
        {
            GameObject go = new GameObject("PlayerData");
            Instance = go.AddComponent<PlayerData>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>씬 종료 시 인스턴스 해제</summary>
    public static void Reset()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }


    // 체력 관련
    public int hp;
    public int maxHP;
    public int hpLostThisTurn;
    public bool hasLostHpThisTurn;

    // 방어력
    public int defense;

    // 에너지
    public int energy;
    public int maxEnergy;

    // 버프/디버프
    public List<Buff> currentBuffs = new List<Buff>();
    public WuppiModeState wuppiMode = WuppiModeState.Normal;

    /// <summary>
    /// 플레이어의 전투 시작 스탯을 초기화
    /// </summary>
    public void Initialize(int startMaxHp, int startDefense = 0, int startMaxEnergy = 3)
    {
        maxHP = startMaxHp;
        hp = maxHP;
        hpLostThisTurn = 0;
        hasLostHpThisTurn = false;
        defense = startDefense;
        maxEnergy = startMaxEnergy;
        energy = maxEnergy;
        currentBuffs.Clear();
        wuppiMode = WuppiModeState.Normal;
        
        Debug.Log($"플레이어 초기화 완료 - HP: {hp}/{maxHP}, 방어력: {defense}, 에너지: {energy}/{maxEnergy}");
    }

    /// <summary>
    /// 방어력 추가
    /// </summary>
    public int AddDefense(int amount)
    {
        int finalAmount = Mathf.Max(0, amount);
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager != null)
        {
            finalAmount = battleManager.ResolvePlayerBarrierGain(finalAmount);
        }

        if (finalAmount <= 0)
        {
            return 0;
        }

        defense += finalAmount;
        Debug.Log($"방어력 +{finalAmount} (현재: {defense})");
        return finalAmount;
    }

    public int RemoveDefense(int amount, bool countAsConsumed = true)
    {
        int removedAmount = Mathf.Clamp(amount, 0, defense);
        if (removedAmount <= 0)
        {
            return 0;
        }

        defense -= removedAmount;
        if (countAsConsumed)
        {
            TrainingBattleManager.Instance?.battleContext?.OnDefenseConsumed(removedAmount);
        }

        TrainingBattleManager.Instance?.HandlePlayerBarrierReduced(removedAmount);
        return removedAmount;
    }

    /// <summary>
    /// 버프 추가
    /// </summary>
    public void AddBuff(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        bool isNonStackable = BuffData.IsNonStackableBuffId(buffId);
        int appliedAmount = isNonStackable ? 1 : amount;
        amount = appliedAmount;

        BuffData data = BuffMetadataResolver.Resolve(buffId);

        Buff existingBuff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);
        if (existingBuff != null)
        {
            existingBuff.data = data;
            if (isNonStackable)
            {
                existingBuff.stack = 1;
            }
            else
            {
                existingBuff.stack += appliedAmount;
            }
            Debug.Log($"버프 중첩: {data.name} (+{amount}) -> {existingBuff.stack}");
        }
        else
        {
            Buff newBuff = new Buff(data, appliedAmount);
            currentBuffs.Add(newBuff);
            Debug.Log($"버프 획득: {data.name} ({amount})");
        }
    }

    /// <summary>
    /// 에너지 추가
    /// </summary>
    public void AddEnergy(int amount)
    {
        energy = Mathf.Min(energy + amount, maxEnergy);
        Debug.Log($"에너지 +{amount} (현재: {energy}/{maxEnergy})");
    }

    /// <summary>
    /// 에너지 사용
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
    /// 피격 (방어력 적용)
    /// </summary>
    public int TakeDamage(int amount, Monster attacker = null)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        int finalDamage = battleManager != null
            ? battleManager.ResolvePlayerIncomingDamage(amount, attacker)
            : Mathf.Max(0, amount);
        if (battleManager != null && battleManager.TryPreventPlayerIncomingDamage(finalDamage, attacker))
        {
            return 0;
        }

        int blockedDamage = Mathf.Min(defense, Mathf.Max(0, finalDamage));
        int damageAfterDefense = Mathf.Max(0, finalDamage - defense);
        hp -= damageAfterDefense;
        hpLostThisTurn += damageAfterDefense;
        if (damageAfterDefense > 0)
        {
            hasLostHpThisTurn = true;
            battleManager?.HandlePlayerHpLost(damageAfterDefense);
        }
        if (blockedDamage > 0)
        {
            RemoveDefense(blockedDamage);
        }

        Debug.Log($"플레이어가 {damageAfterDefense} 데미지를 받았습니다! (HP: {hp}/{maxHP})");
        TryConsumeSoulProtection();
        if (finalDamage > 0)
        {
            battleManager?.ConsumePlayerIncomingDamageBuffs(attacker, finalDamage);
        }
        if (attacker != null && finalDamage > 0)
        {
            battleManager?.HandlePlayerHit(attacker, blockedDamage, damageAfterDefense);
        }

        return damageAfterDefense;
    }

    public int LoseHp(int amount)
    {
        int lostAmount = Mathf.Clamp(amount, 0, hp);
        if (lostAmount <= 0)
        {
            return 0;
        }

        hp -= lostAmount;
        hpLostThisTurn += lostAmount;
        hasLostHpThisTurn = true;
        TrainingBattleManager.Instance?.HandlePlayerHpLost(lostAmount);
        TryConsumeSoulProtection();

        return lostAmount;
    }

    private void TryConsumeSoulProtection()
    {
        TrainingBattleManager.Instance?.TryConsumeSoulProtection();
    }

    /// <summary>
    /// 체력 회복
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
        hpLostThisTurn = 0;
        hasLostHpThisTurn = false;
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        bool keepBarrier = battleManager != null && battleManager.ShouldRetainPlayerBarrierOnTurnStart();
        if (!keepBarrier)
        {
            int removeAmount = battleManager != null ? battleManager.GetPlayerTurnStartBarrierLoss(defense) : defense;
            if (removeAmount > 0)
            {
                RemoveDefense(removeAmount, false);
            }
        }
        energy = maxEnergy; // 에너지 회복
        Debug.Log(keepBarrier
            ? "턴 시작: 보호막 유지 발동, 에너지 회복"
            : "턴 시작: 방어력 리셋, 에너지 회복");
    }

    /// <summary>
    /// 턴 종료 시 처리
    /// </summary>
    public int GetBuffStack(int buffId)
    {
        Buff buff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);
        return buff != null ? buff.stack : 0;
    }

    public float GetOutgoingDamageMultiplier()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        return battleManager != null ? battleManager.GetPlayerOutgoingDamageMultiplier() : 1f;
    }

    public int CalculateCardDamage(int baseDamage, float strengthMultiplier = 1f, float cardBaseDamageMultiplier = 1f)
    {
        int safeBaseDamage = Mathf.Max(0, baseDamage);
        float safeStrengthMultiplier = Mathf.Max(0f, strengthMultiplier);
        float safeCardBaseDamageMultiplier = Mathf.Max(0f, cardBaseDamageMultiplier);
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        int strengthBonus = battleManager != null
            ? battleManager.GetPlayerCalculatedCardDamageBonus(safeStrengthMultiplier)
            : 0;
        float totalMultiplier = battleManager != null
            ? battleManager.GetPlayerCalculatedCardBaseMultiplier(safeCardBaseDamageMultiplier)
            : safeCardBaseDamageMultiplier;
        int damageWithStrength = Mathf.Max(0, Mathf.FloorToInt((safeBaseDamage + strengthBonus) * totalMultiplier));
        return Mathf.Max(0, Mathf.FloorToInt(damageWithStrength * GetOutgoingDamageMultiplier()));
    }

    private void DecreaseBuffStack(int buffId, int amount)
    {
        if (amount <= 0)
            return;

        Buff buff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);
        if (buff == null)
            return;

        buff.stack -= amount;
        if (buff.stack <= 0)
        {
            currentBuffs.Remove(buff);
        }
    }

    public void ConsumeBuffStack(int buffId, int amount)
    {
        DecreaseBuffStack(buffId, amount);
    }

    public void SetBuffStack(int buffId, int amount)
    {
        Buff existingBuff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);

        if (amount <= 0)
        {
            if (existingBuff != null)
            {
                currentBuffs.Remove(existingBuff);
            }
            return;
        }

        BuffData data = BuffMetadataResolver.Resolve(buffId);
        if (existingBuff != null)
        {
            existingBuff.data = data;
            existingBuff.stack = amount;
            return;
        }

        currentBuffs.Add(new Buff(data, amount));
    }

    public void RemoveBuffStack(int buffId)
    {
        RemoveBuff(buffId);
    }

    private void RemoveBuff(int buffId)
    {
        currentBuffs.RemoveAll(b =>
            b.data != null &&
            b.data.buffId == buffId);
    }

    /// <summary>
    /// 플레이어가 죽었는지 확인
    /// </summary>
    public bool IsDead()
    {
        return hp <= 0;
    }
}
