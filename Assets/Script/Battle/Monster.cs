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

    [Header("Stats")]
    public int hp;
    public int maxHP;
    public int defense;
    public int attackPower;
    public new string name;
    public List<Buff> currentBuffs = new();

    private MonsterIntentType plannedIntentType = MonsterIntentType.None;
    private int plannedIntentValue = 0;

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
        if (data == null) return;

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
    }

    public void OnTurnStart()
    {
        // Reserved for buff/debuff start triggers.
    }

    public void OnTurnEnd()
    {
        // Reserved for buff/debuff end triggers.
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
}
