using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum MonsterIntentIconType
{
    None,
    Attack,
    Protection,
    BeneficialEffect,
    HarmfulEffect,
    Summon,
    Bomb,
    DisruptCard,
    Heal,
    Stun,
    Leave
}

/// <summary>
/// Holds monster battle data and turn hooks.
/// </summary>
public abstract class Monster : MonoBehaviour
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

    private bool hasAttackIntent = false;
    private int plannedIntentValue = 0;
    private string plannedIntentDescription = string.Empty;
    private readonly List<MonsterIntentIconType> plannedIntentIcons = new();
    private int plannedPatternId = 0;
    private bool skipCurrentTurnAction = false;
    private bool hasTriggeredDeath = false;
    private bool hasLeftCombat = false;
    private bool hasInitializedBattleStart = false;

    public bool HasAttackIntent => hasAttackIntent;
    public int PlannedIntentValue => plannedIntentValue;
    public int PlannedPatternId => plannedPatternId;
    public IReadOnlyList<MonsterIntentIconType> PlannedIntentIcons => plannedIntentIcons;
    public abstract int MonsterId { get; }
    protected abstract string MonsterName { get; }
    protected abstract int BaseMaxHp { get; }
    protected virtual int BaseAttackPower => 0;
    protected virtual int BaseDefense => 0;
    protected virtual bool IsBossMonster => false;

    protected virtual void Awake()
    {
        InitializeMonsterState();
    }

    protected virtual void Start()
    {
        EnsureIntentTextReference();

        if (TrainingBattleManager.Instance != null)
        {
            TrainingBattleManager.Instance.RegisterMonster(this);
        }

        EnsureBattleStartInitialized();
        UpdateUI();
    }

    protected virtual void OnDestroy()
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

    public void EnsureBattleStartInitialized()
    {
        if (hasInitializedBattleStart)
        {
            return;
        }

        hasInitializedBattleStart = true;
        OnBattleStart();
    }

    public void PlanNextAction()
    {
        EnsureBattleStartInitialized();

        if (IsDead())
        {
            ClearPlannedAction();
            UpdateIntentUI();
            return;
        }

        BuildNextAction();

        Debug.Log($"[Monster] {name} planned action: {plannedIntentDescription}");
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
        EnsureBattleStartInitialized();

        if (target == null || IsDead())
            return;

        if (skipCurrentTurnAction)
        {
            Debug.Log($"[Enemy Turn] {name}은(는) 기절 상태로 이번 턴 행동을 쉬었습니다.");
            skipCurrentTurnAction = false;
            ClearPlannedAction();
            UpdateUI();
            return;
        }

        ExecuteAction(target);

        ClearPlannedAction();
        UpdateUI();
    }

    public int TakeDamage(int amount, int playerStrength = 0)
    {
        int finalDamage = amount + playerStrength;
        finalDamage = ApplyIncomingDamageMultiplier(finalDamage);
        if (finalDamage > 0)
        {
            if (!CanReceiveDamage(finalDamage))
            {
                UpdateUI();
                return 0;
            }

            OnBeforeTakeDamage(finalDamage);
        }
        int defenseBeforeHit = defense;
        int damageAfterDefense = Mathf.Max(0, finalDamage - defenseBeforeHit);

        hp -= damageAfterDefense;
        SetDefenseValue(defenseBeforeHit - finalDamage);

        Debug.Log($"{name} took {damageAfterDefense} damage. (HP: {hp}/{maxHP})");
        if (damageAfterDefense > 0)
        {
            TrainingBattleManager.Instance?.HandleMonsterHpLost(this, damageAfterDefense);
        }

        OnAfterTakeDamage(finalDamage, damageAfterDefense);
        if (finalDamage > 0)
        {
            ConsumeIncomingDamageBuff();
        }
        HandleDeathIfNeeded();
        UpdateUI();
        return damageAfterDefense;
    }

    public void Kill()
    {
        if (IsDead())
            return;

        int lostHp = Mathf.Max(0, hp);
        hp = 0;
        defense = 0;
        if (lostHp > 0)
        {
            TrainingBattleManager.Instance?.HandleMonsterHpLost(this, lostHp);
        }
        HandleDeathIfNeeded();
        UpdateUI();
    }

    public void LeaveCombat()
    {
        if (hasTriggeredDeath || hasLeftCombat)
        {
            return;
        }

        hasLeftCombat = true;
        defense = 0;
        ClearPlannedAction();
        OnLeaveCombatTriggered();
        TrainingBattleManager.Instance?.HandleMonsterLeaveCombat(this);
        Debug.Log($"[Monster] {name} 전투 이탈");
    }

    public void ReviveFromRespawn()
    {
        if (hasLeftCombat)
        {
            return;
        }

        InitializeMonsterState();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        TrainingBattleManager.Instance?.RegisterMonster(this);
        OnRevivedTriggered();
        UpdateUI();
    }

    public void AddDefense(int amount)
    {
        defense += amount;
        Debug.Log($"{name} defense +{amount} (now: {defense})");
        UpdateUI();
    }

    public int RemoveDefense(int amount)
    {
        int removedAmount = Mathf.Clamp(amount, 0, defense);
        if (removedAmount <= 0)
        {
            return 0;
        }

        SetDefenseValue(defense - removedAmount);
        UpdateUI();
        return removedAmount;
    }

    public void SetDefense(int amount)
    {
        SetDefenseValue(amount);
        UpdateUI();
    }

    public void AddBuff(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        BuffData data = BuffManager.Instance != null ? BuffManager.Instance.GetBuffData(buffId) : null;
        if (data == null)
        {
            data = new BuffData
            {
                buffId = buffId,
                name = $"버프 {buffId}",
                description = string.Empty
            };
        }

        Buff existingBuff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);
        if (existingBuff != null)
        {
            existingBuff.data = data;
            if (IsNonStackableBuff(buffId))
            {
                existingBuff.stack = Mathf.Max(existingBuff.stack, 1);
            }
            else
            {
                existingBuff.stack += amount;
            }
            Debug.Log($"[Monster Buff Stack] {data.name} (+{amount}) -> {existingBuff.stack}");
        }
        else
        {
            int initialStack = IsNonStackableBuff(buffId) ? 1 : amount;
            Buff newBuff = new Buff(data, initialStack, 0);
            currentBuffs.Add(newBuff);
            Debug.Log($"[Monster Buff Added] {data.name} ({initialStack})");
        }

        // 디버프/버프 스택 변경 즉시 UI 반영
        if (buffId == BattleRuntimeDefinitions.FreezeBuffId)
        {
            ResolveFreezeThresholdIfNeeded();
        }

        UpdateUI();
    }

    public void OnTurnStart()
    {
        ClearDefenseOnTurnStart();
        OnTurnStarted();
        ResolveFreezeThresholdIfNeeded();
        UpdateUI();
        int freezeStack = GetBuffStack(BattleRuntimeDefinitions.FreezeBuffId);
        freezeStack = Mathf.Min(freezeStack, 6);
        if (freezeStack >= 7)
        {
            DecreaseBuffStack(BattleRuntimeDefinitions.FreezeBuffId, 7);
            skipCurrentTurnAction = true;
            ClearPlannedAction();
            Debug.Log($"[Monster] {name} 빙결 7스택으로 기절 상태가 되어 이번 턴 행동을 쉽니다.");
        }

        UpdateUI();
    }

    public void OnTurnEnd()
    {
        int regeneration = GetBuffStack(3001);
        if (regeneration > 0)
        {
            Heal(regeneration);
            DecreaseBuffStack(3001, 1);
        }

        int burn = GetBuffStack(4003);
        if (burn > 0)
        {
            TakeDamage(burn, 0);
        }

        // 부식(4001), 강화부식(4002)은 턴 종료 시 지속 턴 1 감소
        // 현재는 피격 시마다 부식 계열 스택이 1 감소함
        OnTurnEnded();
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

    public int GetBuffStack(int buffId)
    {
        Buff buff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);
        return buff != null ? buff.stack : 0;
    }

    public void ConsumeBuffStack(int buffId, int amount)
    {
        DecreaseBuffStack(buffId, amount);
        UpdateUI();
    }

    protected void SetAttackIntent(int intentValue, string intentDescription)
    {
        hasAttackIntent = true;
        plannedIntentValue = Mathf.Max(0, intentValue);
        plannedIntentDescription = intentDescription;
    }

    protected void SetIntent(string intentDescription)
    {
        hasAttackIntent = false;
        plannedIntentValue = 0;
        plannedIntentDescription = intentDescription;
    }

    protected void SetPlannedPattern(int patternId, params MonsterIntentIconType[] iconTypes)
    {
        plannedPatternId = Mathf.Max(0, patternId);
        plannedIntentIcons.Clear();

        if (iconTypes == null)
        {
            return;
        }

        foreach (MonsterIntentIconType iconType in iconTypes)
        {
            if (iconType == MonsterIntentIconType.None)
            {
                continue;
            }

            plannedIntentIcons.Add(iconType);
        }
    }

    protected int DealDamage(PlayerData target, int baseDamage)
    {
        if (target == null)
        {
            return 0;
        }

        int finalDamage = ApplyOutgoingDamageModifier(baseDamage);
        Debug.Log($"[Enemy Turn] {name} attacks for {finalDamage}");
        return target.TakeDamage(finalDamage, this);
    }

    protected void AddCardToPlayerDiscard(Card card)
    {
        if (card == null)
        {
            return;
        }

        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager?.usableDeckManager == null)
        {
            return;
        }

        battleManager.usableDeckManager.AddToDiscard(card);
        battleManager.UpdateAllUI();
        Debug.Log($"[Monster] {name}이(가) {card.cardName} 카드를 버린 카드 더미에 생성했습니다.");
    }

    private void UpdateIntentUI()
    {
        EnsureIntentTextReference();
        if (intentText == null)
            return;

        if (!string.IsNullOrWhiteSpace(plannedIntentDescription))
        {
            intentText.text = plannedIntentDescription;
        }
        else if (hasAttackIntent)
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

        int enhancedCorrosionStack = GetBuffStack(BattleRuntimeDefinitions.EnhancedCorrosionBuffId);
        int corrosionStack = GetBuffStack(BattleRuntimeDefinitions.CorrosionBuffId);

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

        int freezeStack = GetBuffStack(BattleRuntimeDefinitions.FreezeBuffId);
        freezeText.text = $"FreezeStack : {freezeStack}";
    }

    private int ApplyIncomingDamageMultiplier(int incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        float multiplier = 1f;

        // 강화부식이 있으면 50%, 아니면 부식 25%
        bool hasEnhancedCorrosion = GetBuffStack(BattleRuntimeDefinitions.EnhancedCorrosionBuffId) > 0;
        bool hasCorrosion = GetBuffStack(BattleRuntimeDefinitions.CorrosionBuffId) > 0;
        bool playerEnhancesCorrosion = !hasEnhancedCorrosion
            && hasCorrosion
            && TrainingBattleManager.Instance?.playerData != null
            && TrainingBattleManager.Instance.playerData.GetBuffStack(BattleRuntimeDefinitions.CorrosionEnhanceBuffId) > 0;

        if (hasEnhancedCorrosion || playerEnhancesCorrosion)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(BattleRuntimeDefinitions.EnhancedCorrosionBuffId, 1.5f)
                : 1.5f;
        }
        else if (hasCorrosion)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(BattleRuntimeDefinitions.CorrosionBuffId, 1.25f)
                : 1.25f;
        }

        return Mathf.FloorToInt(incomingDamage * multiplier);
    }

    private void ConsumeIncomingDamageBuff()
    {
        if (GetBuffStack(BattleRuntimeDefinitions.EnhancedCorrosionBuffId) > 0)
        {
            DecreaseBuffStack(BattleRuntimeDefinitions.EnhancedCorrosionBuffId, 1);
            return;
        }

        if (GetBuffStack(BattleRuntimeDefinitions.CorrosionBuffId) > 0)
        {
            DecreaseBuffStack(BattleRuntimeDefinitions.CorrosionBuffId, 1);
        }
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

    private void RemoveBuff(int buffId)
    {
        currentBuffs.RemoveAll(buff =>
            buff.data != null &&
            buff.data.buffId == buffId);
    }

    private void ResolveFreezeThresholdIfNeeded()
    {
        int freezeStack = GetBuffStack(BattleRuntimeDefinitions.FreezeBuffId);
        if (freezeStack < 7)
        {
            return;
        }

        while (freezeStack >= 7 && !IsDead())
        {
            DecreaseBuffStack(BattleRuntimeDefinitions.FreezeBuffId, 7);
            freezeStack -= 7;

            if (IsBossMonster)
            {
                Debug.Log($"[Monster] {name}은 빙결 7스택으로 대신 20 피해를 받습니다.");
                TakeDamage(20, 0);
                continue;
            }

            skipCurrentTurnAction = true;
            ClearPlannedAction();
            Debug.Log($"[Monster] {name} 빙결 7스택으로 기절 상태가 되어 이번 턴 행동을 쉽니다.");
            break;
        }
    }

    private void InitializeMonsterState()
    {
        currentBuffs ??= new List<Buff>();
        currentBuffs.Clear();

        maxHP = BaseMaxHp;
        hp = BaseMaxHp;
        defense = BaseDefense;
        attackPower = BaseAttackPower;
        name = MonsterName;

        skipCurrentTurnAction = false;
        hasTriggeredDeath = false;
        hasLeftCombat = false;
        ClearPlannedAction();
    }

    private void HandleDeathIfNeeded()
    {
        if (hp > 0 || hasTriggeredDeath)
        {
            return;
        }

        hasTriggeredDeath = true;
        defense = 0;
        ClearPlannedAction();
        OnDeathTriggered();
        TrainingBattleManager.Instance?.HandleMonsterDeath(this);
        Debug.Log($"[Monster] {name} 처치");
    }

    private void ClearPlannedAction()
    {
        hasAttackIntent = false;
        plannedIntentValue = 0;
        plannedIntentDescription = string.Empty;
        plannedPatternId = 0;
        plannedIntentIcons.Clear();
    }

    private void SetDefenseValue(int amount)
    {
        int previousDefense = defense;
        defense = Mathf.Max(0, amount);
        HandleRootedBarrierBreak(previousDefense);
    }

    private void ClearDefenseOnTurnStart()
    {
        if (defense <= 0 || GetBuffStack(BattleRuntimeDefinitions.RootedBuffId) > 0)
        {
            return;
        }

        defense = 0;
        Debug.Log($"[Monster] {name} 턴 시작으로 보호막이 제거됩니다.");
    }

    private void HandleRootedBarrierBreak(int previousDefense)
    {
        if (previousDefense <= 0 || defense > 0 || hp <= 0)
        {
            return;
        }

        if (GetBuffStack(BattleRuntimeDefinitions.RootedBuffId) <= 0)
        {
            return;
        }

        RemoveBuff(BattleRuntimeDefinitions.RootedBuffId);
        skipCurrentTurnAction = true;
        SetIntent("기절합니다.");
        SetPlannedPattern(0, MonsterIntentIconType.Stun);
        Debug.Log($"[Monster] {name} 뿌리내림을 잃고 기절합니다.");
    }

    private int ApplyOutgoingDamageModifier(int baseDamage)
    {
        if (baseDamage <= 0)
        {
            return 0;
        }

        return Mathf.Max(0, baseDamage + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
    }

    private void EnsureIntentTextReference()
    {
        if (intentText != null)
        {
            return;
        }

        TextMeshProUGUI sourceText = hpText != null ? hpText : corrosionText;
        if (sourceText == null)
        {
            return;
        }

        intentText = Instantiate(sourceText, sourceText.transform.parent);
        intentText.gameObject.name = "EnemyIntentText";
        intentText.text = string.Empty;

        RectTransform sourceRect = sourceText.rectTransform;
        RectTransform intentRect = intentText.rectTransform;
        intentRect.anchorMin = sourceRect.anchorMin;
        intentRect.anchorMax = sourceRect.anchorMax;
        intentRect.pivot = sourceRect.pivot;
        intentRect.sizeDelta = sourceRect.sizeDelta;
        intentRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -40f);
        intentRect.localScale = sourceRect.localScale;

        int siblingIndex = sourceText.transform.GetSiblingIndex();
        intentText.transform.SetSiblingIndex(siblingIndex + 1);
    }

    protected virtual bool IsNonStackableBuff(int buffId)
    {
        return false;
    }

    protected virtual void OnBattleStart()
    {
    }

    protected virtual void OnDeathTriggered()
    {
    }

    protected virtual void OnLeaveCombatTriggered()
    {
    }

    protected virtual void OnTurnStarted()
    {
    }

    protected virtual void OnBeforeTakeDamage(int incomingDamage)
    {
    }

    protected virtual void OnAfterTakeDamage(int incomingDamage, int damageAfterDefense)
    {
    }

    protected virtual void OnTurnEnded()
    {
    }

    protected virtual void OnRevivedTriggered()
    {
    }

    protected virtual bool CanReceiveDamage(int incomingDamage)
    {
        return true;
    }

    protected abstract void BuildNextAction();
    protected abstract void ExecuteAction(PlayerData target);
}
