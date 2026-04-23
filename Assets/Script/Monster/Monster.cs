using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
public abstract class Monster : MonoBehaviour, IPointerClickHandler
{
    private static readonly Color SelectionTintColor = new Color(0.45f, 0.75f, 1f, 1f);

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

    private bool hasAttackIntent = false;
    private int plannedIntentValue = 0;
    private string plannedIntentDescription = string.Empty;
    private readonly List<MonsterIntentIconType> plannedIntentIcons = new();
    private int plannedPatternId = 0;
    private bool skipCurrentTurnAction = false;
    private bool hasTriggeredDeath = false;
    private bool hasLeftCombat = false;
    private bool hasInitializedBattleStart = false;
    private bool isSelectionHighlighted = false;
    private int lastSelectionClickFrame = -1;
    private readonly Dictionary<Graphic, Color> originalGraphicColors = new();
    private readonly Dictionary<SpriteRenderer, Color> originalSpriteColors = new();

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
    public bool IsBoss => IsBossMonster;

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
        SetSelectionHighlight(false);

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
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        int finalDamage = amount + playerStrength;
        finalDamage = battleManager != null
            ? battleManager.ResolveMonsterIncomingDamage(this, finalDamage)
            : Mathf.Max(0, finalDamage);
        if (finalDamage > 0)
        {
            if (!CanReceiveDamage(finalDamage) || !(battleManager?.CanMonsterReceiveDamage(this, finalDamage) ?? true))
            {
                UpdateUI();
                return 0;
            }

            battleManager?.HandleMonsterBeforeTakeDamage(this, finalDamage);
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
            battleManager?.HandleMonsterAfterTakeDamage(this, finalDamage, damageAfterDefense);
        }
        if (finalDamage > 0)
        {
            battleManager?.ConsumeMonsterIncomingDamageBuffs(this, finalDamage);
        }
        HandleDeathIfNeeded();
        UpdateUI();
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
        TrainingBattleManager.Instance?.HandleMonsterHpLost(this, lostAmount);
        HandleDeathIfNeeded();
        UpdateUI();
        return lostAmount;
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
        TrainingBattleManager.Instance?.HandleMonsterRevived(this);
        UpdateUI();
    }

    public void AddDefense(int amount)
    {
        int finalAmount = amount;
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager != null)
        {
            finalAmount = battleManager.ResolveMonsterBarrierGain(this, amount);
        }

        if (finalAmount <= 0)
        {
            return;
        }

        defense += finalAmount;
        Debug.Log($"{name} defense +{finalAmount} (now: {defense})");
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

        bool isNonStackable = IsNonStackableBuff(buffId);
        int appliedAmount = isNonStackable ? 1 : amount;

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
            Debug.Log($"[Monster Buff Stack] {data.name} (+{appliedAmount}) -> {existingBuff.stack}");
        }
        else
        {
            Buff newBuff = new Buff(data, appliedAmount);
            currentBuffs.Add(newBuff);
            Debug.Log($"[Monster Buff Added] {data.name} ({appliedAmount})");
        }

        // 디버프/버프 스택 변경 즉시 UI 반영
        TrainingBattleManager.Instance?.HandleMonsterBuffApplied(this, buffId, appliedAmount);

        UpdateUI();
    }

    public void OnTurnStart()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (defense > 0 && !(battleManager?.ShouldKeepMonsterBarrierOnTurnStart(this) ?? false))
        {
            defense = 0;
            Debug.Log($"[Monster] {name} 턴 시작으로 보호막이 제거됩니다.");
        }
        OnTurnStarted();
        battleManager?.ApplyMonsterTurnStartEffects(this);
        UpdateUI();
    }

    public void OnTurnEnd()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        battleManager?.ApplyMonsterTurnEndEffects(this);
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

    public void RemoveBuffStack(int buffId)
    {
        RemoveBuff(buffId);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryHandleSelectionClick();
    }

    public void SetSelectionHighlight(bool isHighlighted)
    {
        if (isSelectionHighlighted == isHighlighted)
        {
            return;
        }

        isSelectionHighlighted = isHighlighted;
        ApplySelectionHighlight();
    }

    public void SkipCurrentTurnActionOnce()
    {
        skipCurrentTurnAction = true;
        ClearPlannedAction();
    }

    public void SetStunIntent()
    {
        skipCurrentTurnAction = true;
        SetIntent("기절합니다.");
        SetPlannedPattern(0, MonsterIntentIconType.Stun);
    }

    public void ScalePlannedIntent(float multiplier)
    {
        if (multiplier < 0f || !hasAttackIntent || plannedIntentValue <= 0)
        {
            return;
        }

        int previousValue = plannedIntentValue;
        plannedIntentValue = Mathf.Max(0, Mathf.FloorToInt(plannedIntentValue * multiplier));
        plannedIntentDescription = ReplaceIntentValue(plannedIntentDescription, previousValue, plannedIntentValue);
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
        int hpDamage = target.TakeDamage(finalDamage, this);
        TrainingBattleManager.Instance?.HandleMonsterAttackResolved(this, target, finalDamage, hpDamage);
        return hpDamage;
    }

    protected int PreviewOutgoingDamage(int baseDamage)
    {
        return ApplyOutgoingDamageModifier(baseDamage);
    }

    protected int PreviewBarrierGain(int baseAmount)
    {
        int safeAmount = Mathf.Max(0, baseAmount);
        return TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ResolveMonsterBarrierGain(this, safeAmount)
            : safeAmount;
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

    private static string ReplaceIntentValue(string description, int previousValue, int nextValue)
    {
        if (string.IsNullOrWhiteSpace(description) || previousValue < 0)
        {
            return description;
        }

        string previousText = previousValue.ToString();
        int replaceIndex = description.IndexOf(previousText, System.StringComparison.Ordinal);
        if (replaceIndex < 0)
        {
            return description;
        }

        return description.Substring(0, replaceIndex)
            + nextValue
            + description.Substring(replaceIndex + previousText.Length);
    }

    private void SetDefenseValue(int amount)
    {
        int previousDefense = defense;
        defense = Mathf.Max(0, amount);
        TrainingBattleManager.Instance?.HandleMonsterDefenseChanged(this, previousDefense, defense);
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TryHandleSelectionClick();
    }

    private void TryHandleSelectionClick()
    {
        if (lastSelectionClickFrame == Time.frameCount)
        {
            return;
        }

        lastSelectionClickFrame = Time.frameCount;
        TrainingBattleManager.Instance?.HandleMonsterClicked(this);
    }

    private void ApplySelectionHighlight()
    {
        ApplyGraphicSelectionHighlight();
        ApplySpriteSelectionHighlight();
    }

    private void ApplyGraphicSelectionHighlight()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
            {
                continue;
            }

            if (!originalGraphicColors.ContainsKey(graphic))
            {
                originalGraphicColors[graphic] = graphic.color;
            }

            Color originalColor = originalGraphicColors[graphic];
            graphic.color = isSelectionHighlighted
                ? BlendSelectionColor(originalColor)
                : originalColor;
        }
    }

    private void ApplySpriteSelectionHighlight()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            if (!originalSpriteColors.ContainsKey(spriteRenderer))
            {
                originalSpriteColors[spriteRenderer] = spriteRenderer.color;
            }

            Color originalColor = originalSpriteColors[spriteRenderer];
            spriteRenderer.color = isSelectionHighlighted
                ? BlendSelectionColor(originalColor)
                : originalColor;
        }
    }

    private static Color BlendSelectionColor(Color originalColor)
    {
        Color tintedColor = Color.Lerp(originalColor, SelectionTintColor, 0.65f);
        tintedColor.a = originalColor.a;
        return tintedColor;
    }

    private int ApplyOutgoingDamageModifier(int baseDamage)
    {
        if (baseDamage <= 0)
        {
            return 0;
        }

        return TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ModifyMonsterOutgoingDamage(this, baseDamage)
            : Mathf.Max(0, baseDamage);
    }

    private void EnsureIntentTextReference()
    {
        if (intentText != null)
        {
            return;
        }

        TextMeshProUGUI sourceText = hpText != null ? hpText : defenseText;
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
        return BuffData.IsNonStackableBuffId(buffId);
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
