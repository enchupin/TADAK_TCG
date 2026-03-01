using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum MonsterIntentType
{
    None,
    Attack
}

/// <summary>
/// Holds monster battle data and turn hooks.
/// </summary>
public class Monster : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private TextMeshProUGUI corrosionText;
    [SerializeField] private TextMeshProUGUI freezeText;

    [Header("Stats")]
    public int hp;
    public int maxHP;
    public int defense;
    public int attackPower;
    public new string name;
    public List<Buff> currentBuffs = new();

    private MonsterIntentType plannedIntentType = MonsterIntentType.None;
    private int plannedIntentValue = 0;
    private bool skipCurrentTurnAction = false;

    public MonsterIntentType PlannedIntentType => plannedIntentType;
    public int PlannedIntentValue => plannedIntentValue;

    private void Awake()
    {
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
        if (TrainingBattleManager.Instance != null)
        {
            TrainingBattleManager.Instance.RegisterMonster(this);
        }
    }

    private void OnDestroy()
    {
        if (TrainingBattleManager.Instance != null)
        {
            TrainingBattleManager.Instance.UnregisterMonster(this);
        }
    }

    public void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"HP : {hp}/{maxHP}";

        if (defenseText != null)
            defenseText.text = defense > 0 ? $"DEF {defense}" : string.Empty;

        UpdateIntentUI();
        UpdateCorrosionUI();
        UpdateFreezeUI();
    }

    public void PlanNextAction()
    {
        if (IsDead())
        {
            plannedIntentType = MonsterIntentType.None;
            plannedIntentValue = 0;
            UpdateIntentUI();
            return;
        }

        plannedIntentType = MonsterIntentType.Attack;
        int min = Mathf.Max(1, attackPower - 2);
        int maxExclusive = Mathf.Max(min + 1, attackPower + 3);
        plannedIntentValue = Random.Range(min, maxExclusive);

        Debug.Log($"[Monster] {name} planned action: {plannedIntentType} {plannedIntentValue}");
        UpdateIntentUI();
    }

    /// <summary>
    /// Compatibility method for existing calls.
    /// </summary>
    public void EnemyTurn(PlayerData target)
    {
        PlanNextAction();
        ExecutePlannedAction(target);
    }

    public void ExecutePlannedAction(PlayerData target)
    {
        if (target == null || IsDead())
            return;

        if (skipCurrentTurnAction)
        {
            Debug.Log($"[Enemy Turn] {name}은(는) 기절 상태로 이번 턴 행동을 쉬었습니다.");
            skipCurrentTurnAction = false;
            plannedIntentType = MonsterIntentType.None;
            plannedIntentValue = 0;
            UpdateUI();
            return;
        }

        switch (plannedIntentType)
        {
            case MonsterIntentType.Attack:
                Debug.Log($"[Enemy Turn] {name} attacks for {plannedIntentValue}");
                target.TakeDamage(plannedIntentValue);
                break;
            case MonsterIntentType.None:
            default:
                break;
        }

        plannedIntentType = MonsterIntentType.None;
        plannedIntentValue = 0;
        UpdateIntentUI();
    }

    public int TakeDamage(int amount, int playerStrength = 0)
    {
        int finalDamage = amount + playerStrength;
        finalDamage = ApplyIncomingDamageMultiplier(finalDamage);
        int damageAfterDefense = Mathf.Max(0, finalDamage - defense);

        hp -= damageAfterDefense;
        defense = Mathf.Max(0, defense - finalDamage);

        Debug.Log($"{name} took {damageAfterDefense} damage. (HP: {hp}/{maxHP})");
        UpdateUI();
        return damageAfterDefense;
    }

    public void AddDefense(int amount)
    {
        defense += amount;
        Debug.Log($"{name} defense +{amount} (now: {defense})");
        UpdateUI();
    }

    public void AddBuff(int buffId, int amount)
    {
        BuffData data = BuffManager.Instance.GetBuffData(buffId);
        if (data == null)
        {
            data = new BuffData
            {
                buffId = buffId,
                name = $"버프 {buffId}",
                buffType = 0,
                description = string.Empty
            };
        }

        Buff existingBuff = currentBuffs.Find(b => b.data.buffId == buffId);
        if (existingBuff != null)
        {
            existingBuff.stack += amount;
            Debug.Log($"[Monster Buff Stack] {data.name} (+{amount}) -> {existingBuff.stack}");
        }
        else
        {
            Buff newBuff = new Buff(data, amount, 0);
            currentBuffs.Add(newBuff);
            Debug.Log($"[Monster Buff Added] {data.name} ({amount})");
        }

        // 디버프/버프 스택 변경 즉시 UI 반영
        UpdateUI();
    }

    public void OnTurnStart()
    {
        int freezeStack = GetBuffStack(4004);
        if (freezeStack >= 7)
        {
            DecreaseBuffStack(4004, 7);
            skipCurrentTurnAction = true;
            plannedIntentType = MonsterIntentType.None;
            plannedIntentValue = 0;
            Debug.Log($"[Monster] {name} 빙결 7스택으로 기절 상태가 되어 이번 턴 행동을 쉽니다.");
        }

        UpdateUI();
    }

    public void OnTurnEnd()
    {
        // 부식(4001), 강화부식(4002)은 턴 종료 시 지속 턴 1 감소
        DecreaseBuffStack(4001, 1);
        DecreaseBuffStack(4002, 1);
        UpdateUI();
    }

    public bool IsDead()
    {
        return hp <= 0;
    }

    public void Heal(int amount)
    {
        int healAmount = Mathf.Min(amount, maxHP - hp);
        hp += healAmount;
        Debug.Log($"{name} healed +{healAmount} (now: {hp}/{maxHP})");
        UpdateUI();
    }

    private void UpdateIntentUI()
    {
        if (intentText == null)
            return;

        if (plannedIntentType == MonsterIntentType.Attack)
        {
            intentText.text = $"Intent: Attack {plannedIntentValue}";
        }
        else
        {
            intentText.text = string.Empty;
        }
    }

    private void UpdateCorrosionUI()
    {
        if (corrosionText == null)
            return;

        int enhancedCorrosionStack = GetBuffStack(4002);
        int corrosionStack = GetBuffStack(4001);

        if (enhancedCorrosionStack > 0)
        {
            corrosionText.text = $"EcorrosionStack : {enhancedCorrosionStack}";
            return;
        }

        corrosionText.text = $"corrosionStack : {corrosionStack}";
    }

    private void UpdateFreezeUI()
    {
        if (freezeText == null)
            return;

        int freezeStack = GetBuffStack(4004);
        freezeText.text = $"FreezeStack : {freezeStack}";
    }

    private int GetBuffStack(int buffId)
    {
        Buff buff = currentBuffs.Find(b => b.data != null && b.data.buffId == buffId);
        return buff != null ? buff.stack : 0;
    }

    private int ApplyIncomingDamageMultiplier(int incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        float multiplier = 1f;

        // 강화부식이 있으면 50%, 아니면 부식 25%
        if (GetBuffStack(4002) > 0)
        {
            multiplier = 1.5f;
        }
        else if (GetBuffStack(4001) > 0)
        {
            multiplier = 1.25f;
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
}
