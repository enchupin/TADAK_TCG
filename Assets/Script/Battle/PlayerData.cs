using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 전투 관련 데이터를 관리하는 클래스
/// </summary>
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
        
        Debug.Log($"플레이어 초기화 완료 - HP: {hp}/{maxHP}, 방어력: {defense}, 에너지: {energy}/{maxEnergy}");
    }

    /// <summary>
    /// 방어력 추가
    /// </summary>
    public int AddDefense(int amount)
    {
        int finalAmount = Mathf.Max(0, amount);
        if (finalAmount <= 0)
        {
            return 0;
        }

        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager != null)
        {
            finalAmount += battleManager.GetAdditionalBarrierGain();
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
        BuffData data = BuffManager.Instance != null ? BuffManager.Instance.GetBuffData(buffId) : null;
        if (data == null)
        {
            data = new BuffData
            {
                buffId = buffId,
                name = $"버프 {buffId}",
                buffType = BuffData.GetPolarityBuffType(buffId),
                description = string.Empty
            };
        }

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
        int finalDamage = ApplyIncomingDamageMultiplier(amount);
        finalDamage = ApplyDamageClamp(finalDamage);
        if (TryConsumeEvade(finalDamage, attacker))
        {
            return 0;
        }
        if (TryConsumeLavaBarrier(finalDamage, attacker))
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
        }
        if (blockedDamage > 0)
        {
            RemoveDefense(blockedDamage);
        }

        Debug.Log($"플레이어가 {damageAfterDefense} 데미지를 받았습니다! (HP: {hp}/{maxHP})");
        if (attacker != null && finalDamage > 0)
        {
            TrainingBattleManager.Instance?.HandlePlayerHit(attacker, blockedDamage, damageAfterDefense);
        }

        return damageAfterDefense;
    }

    private int ApplyDamageClamp(int finalDamage)
    {
        if (finalDamage <= 0 || GetBuffStack(BattleRuntimeDefinitions.DamageClampToOneBuffId) <= 0)
        {
            return finalDamage;
        }

        return 1;
    }

    private bool TryConsumeEvade(int finalDamage, Monster attacker)
    {
        if (attacker == null || finalDamage <= 0 || GetBuffStack(BattleRuntimeDefinitions.EvadeBuffId) <= 0)
        {
            return false;
        }

        DecreaseBuffStack(BattleRuntimeDefinitions.EvadeBuffId, 1);
        Debug.Log("회피가 발동해 공격을 피했습니다");
        TrainingBattleManager.Instance?.HandlePlayerHit(attacker, 0, 0);
        return true;
    }

    private bool TryConsumeLavaBarrier(int finalDamage, Monster attacker)
    {
        if (finalDamage <= 0 || GetBuffStack(BattleRuntimeDefinitions.LavaBarrierBuffId) <= 0)
        {
            return false;
        }

        DecreaseBuffStack(BattleRuntimeDefinitions.LavaBarrierBuffId, 1);
        Debug.Log("용암 보호막이 발동해 피해를 받지 않았습니다");
        if (attacker != null)
        {
            TrainingBattleManager.Instance?.HandlePlayerHit(attacker, 0, 0);
        }

        return true;
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
        bool keepBarrier = ConsumeBarrierRetentionOnTurnStart();
        if (!keepBarrier)
        {
            if (GetBuffStack(BattleRuntimeDefinitions.EfficientBarrierBuffId) > 0)
            {
                RemoveDefense(15, false);
            }
            else if (defense > 0)
            {
                RemoveDefense(defense, false);
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
    public void OnTurnEnd()
    {
        int regeneration = GetBuffStack(BattleRuntimeDefinitions.RegenerationBuffId);
        if (regeneration > 0)
        {
            Heal(regeneration);
            DecreaseBuffStack(BattleRuntimeDefinitions.RegenerationBuffId, 1);
        }

        int burn = GetBuffStack(BattleRuntimeDefinitions.BurnBuffId);
        if (burn > 0)
        {
            TakeDamage(burn);
        }

        int overheatDecay = GetBuffStack(4007);
        if (overheatDecay > 0)
        {
            DecreaseBuffStack(3017, overheatDecay);
            RemoveBuff(4007);
        }

        int counterattackDecay = GetBuffStack(BattleRuntimeDefinitions.CounterattackDecayBuffId);
        if (counterattackDecay > 0)
        {
            DecreaseBuffStack(3021, counterattackDecay);
            RemoveBuff(BattleRuntimeDefinitions.CounterattackDecayBuffId);
        }

        int damageAmplifyDecay = GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyDecayBuffId);
        if (damageAmplifyDecay > 0)
        {
            DecreaseBuffStack(3002, damageAmplifyDecay);
            RemoveBuff(BattleRuntimeDefinitions.DamageAmplifyDecayBuffId);
        }

        DecreaseBuffStack(BattleRuntimeDefinitions.CardUseAllEnemiesDamageBuffId, 1);
        DecreaseBuffStack(BattleRuntimeDefinitions.DamageClampToOneBuffId, 1);
        RemoveBuff(BattleRuntimeDefinitions.DrawLockBuffId);

        DecreaseBuffStack(BattleRuntimeDefinitions.CorrosionBuffId, 1);
        DecreaseBuffStack(BattleRuntimeDefinitions.EnhancedCorrosionBuffId, 1);
        DecreaseBuffStack(BattleRuntimeDefinitions.WeakBuffId, 1);
    }

    public int GetBuffStack(int buffId)
    {
        Buff buff = currentBuffs.Find(b => b.data != null && b.data.buffId == buffId);
        return buff != null ? buff.stack : 0;
    }

    private bool ConsumeBarrierRetentionOnTurnStart()
    {
        if (TrainingBattleManager.Instance != null && TrainingBattleManager.Instance.HasPermanentBarrierRetention())
        {
            return true;
        }

        if (GetBuffStack(BattleRuntimeDefinitions.BarrierRetentionBuffId) <= 0)
        {
            return false;
        }

        DecreaseBuffStack(BattleRuntimeDefinitions.BarrierRetentionBuffId, 1);
        return true;
    }

    public float GetOutgoingDamageMultiplier()
    {
        if (GetBuffStack(BattleRuntimeDefinitions.WeakBuffId) > 0)
        {
            return BuffManager.Instance != null
                ? BuffManager.Instance.GetOutgoingDamageMultiplier(BattleRuntimeDefinitions.WeakBuffId, 0.75f)
                : 0.75f;
        }

        return 1f;
    }

    public int CalculateCardDamage(int baseDamage, float damageAmplifyMultiplier = 1f, float cardBaseDamageMultiplier = 1f)
    {
        int safeBaseDamage = Mathf.Max(0, baseDamage);
        float safeDamageAmplifyMultiplier = Mathf.Max(0f, damageAmplifyMultiplier);
        float safeCardBaseDamageMultiplier = Mathf.Max(0f, cardBaseDamageMultiplier);
        int damageAmplifyBonus = Mathf.Max(0, Mathf.FloorToInt(
            GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId) * safeDamageAmplifyMultiplier));
        float overheatMultiplier = Mathf.Max(0, GetBuffStack(BattleRuntimeDefinitions.OverheatBuffId)) * 0.1f;
        float totalMultiplier = safeCardBaseDamageMultiplier + overheatMultiplier;
        int amplifiedDamage = Mathf.Max(0, Mathf.FloorToInt((safeBaseDamage + damageAmplifyBonus) * totalMultiplier));
        return Mathf.Max(0, Mathf.FloorToInt(amplifiedDamage * GetOutgoingDamageMultiplier()));
    }

    private int ApplyIncomingDamageMultiplier(int incomingDamage)
    {
        if (incomingDamage <= 0)
        {
            return 0;
        }

        float multiplier = 1f;
        if (GetBuffStack(BattleRuntimeDefinitions.EnhancedCorrosionBuffId) > 0)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(BattleRuntimeDefinitions.EnhancedCorrosionBuffId, 1.5f)
                : 1.5f;
        }
        else if (GetBuffStack(BattleRuntimeDefinitions.CorrosionBuffId) > 0)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(BattleRuntimeDefinitions.CorrosionBuffId, 1.25f)
                : 1.25f;
        }

        return Mathf.FloorToInt(incomingDamage * multiplier);
    }

    private void DecreaseBuffStack(int buffId, int amount)
    {
        if (amount <= 0)
            return;

        Buff buff = currentBuffs.Find(b => b.data != null && b.data.buffId == buffId);
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

    public void RemoveBuffStack(int buffId)
    {
        RemoveBuff(buffId);
    }

    private void RemoveBuff(int buffId)
    {
        currentBuffs.RemoveAll(b => b.data != null && b.data.buffId == buffId);
    }

    /// <summary>
    /// 플레이어가 죽었는지 확인
    /// </summary>
    public bool IsDead()
    {
        return hp <= 0;
    }
}
