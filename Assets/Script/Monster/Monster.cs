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
    private const string HealthBarObjectName = "HpBar";
    private const string HealthBarBackgroundObjectName = "Background";
    private const string HealthBarFillAreaObjectName = "Fill Area";
    private const string HealthBarFillObjectName = "Fill";
    private const string BuffRootObjectName = "Buff";

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private BuffUI buffUI;
    [SerializeField] private Color barrierHPFillColor = new Color32(135, 206, 235, 255);

    [Header("상태 컨트롤러")]
    [SerializeField] private MonsterStateController stateController;

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
    private Color defaultHPFillColor = Color.white;
    private bool hasDefaultHPFillColor = false;
    private readonly Dictionary<Graphic, Color> originalGraphicColors = new();
    private readonly Dictionary<SpriteRenderer, Color> originalSpriteColors = new();

    public bool HasAttackIntent => hasAttackIntent;
    public int PlannedIntentValue => plannedIntentValue;
    public int PlannedPatternId => plannedPatternId;
    public IReadOnlyList<MonsterIntentIconType> PlannedIntentIcons => plannedIntentIcons;
    public MonsterStateType CurrentState => stateController != null
        ? stateController.CurrentState
        : throw new System.InvalidOperationException($"[Monster] {gameObject.name}의 MonsterStateController가 인스펙터에 연결되지 않았습니다");
    public abstract int MonsterId { get; }
    protected virtual string MonsterName => GetType().Name;
    protected abstract int BaseMaxHp { get; }
    protected virtual int BaseAttackPower => 0;
    protected virtual int BaseDefense => 0;
    protected virtual bool IsBossMonster => false;
    public bool IsBoss => IsBossMonster;
    protected float InfiniteStatMultiplier => InfiniteMode.MonsterStatMultiplier;

    protected virtual void Awake()
    {
        InitializeMonsterState();
        if (stateController != null)
        {
            BindStateController();
            stateController.EnterIdle();
        }
    }

    protected virtual void Start()
    {
        BindStateController();
        EnsureIntentTextReference();
        EnsureStatusUIReferences();

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
        EnsureStatusUIReferences();

        if (hpText != null)
            hpText.text = $"HP : {hp}/{maxHP}";

        if (defenseText != null)
            defenseText.text = defense > 0 ? $"DEF {defense}" : string.Empty;

        UpdateHealthBarUI();
        UpdateBuffUI();
        UpdateIntentUI();
    }

    public void SetStateController(MonsterStateController controller)
    {
        if (controller == null)
        {
            throw new System.ArgumentNullException(nameof(controller), "[Monster] MonsterStateController가 비어 있습니다");
        }

        stateController = controller;
        BindStateController();
        stateController.EnterIdle();
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

        if (hasAttackIntent)
        {
            stateController.EnterAttack();
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
        if (finalDamage > 0)
        {
            stateController.EnterHit();
        }

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
        BindStateController();
        stateController.EnterIdle();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        TrainingBattleManager.Instance?.RegisterMonster(this);
        OnRevivedTriggered();
        TrainingBattleManager.Instance?.HandleMonsterRevived(this);
        UpdateUI();
    }

    public void AddDefense(int amount, bool applyInfiniteScaling = true)
    {
        int finalAmount = Mathf.Max(0, amount);
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager != null)
        {
            finalAmount = battleManager.ResolveMonsterBarrierGain(this, finalAmount);
        }

        if (applyInfiniteScaling)
        {
            finalAmount = ScaleInfiniteMonsterValue(finalAmount);
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
        int resolvedAmount = ResolveMonsterBuffAmount(buffId, amount, isNonStackable);
        int appliedAmount = isNonStackable ? 1 : resolvedAmount;

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

    public void Heal(int amount, bool applyInfiniteScaling = false)
    {
        int finalAmount = applyInfiniteScaling ? ScaleInfiniteMonsterValue(amount) : Mathf.Max(0, amount);
        int healAmount = Mathf.Min(finalAmount, maxHP - hp);
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

        int finalDamage = PreviewOutgoingDamage(baseDamage);
        Debug.Log($"[Enemy Turn] {name} attacks for {finalDamage}");
        int hpDamage = target.TakeDamage(finalDamage, this);
        TrainingBattleManager.Instance?.HandleMonsterAttackResolved(this, target, finalDamage, hpDamage);
        return hpDamage;
    }

    protected int PreviewOutgoingDamage(int baseDamage)
    {
        return ScaleInfiniteMonsterValue(ApplyOutgoingDamageModifier(baseDamage));
    }

    protected int PreviewBarrierGain(int baseAmount)
    {
        int safeAmount = Mathf.Max(0, baseAmount);
        int resolvedAmount = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ResolveMonsterBarrierGain(this, safeAmount)
            : safeAmount;
        return ScaleInfiniteMonsterValue(resolvedAmount);
    }

    protected int PreviewMonsterBuffAmount(int buffId, int amount)
    {
        return ResolveMonsterBuffAmount(buffId, amount, IsNonStackableBuff(buffId));
    }

    protected int PreviewPlayerDebuffAmount(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        if (BuffData.IsNonStackableBuffId(buffId))
        {
            return 1;
        }

        return BuffData.IsBeneficialBuffId(buffId)
            ? Mathf.Max(0, amount)
            : ScaleInfiniteMonsterValue(amount);
    }

    protected void AddDebuffToPlayer(PlayerData target, int buffId, int amount)
    {
        target?.AddBuff(buffId, amount, true);
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

    private void EnsureStatusUIReferences()
    {
        EnsureHpSliderReference();
        EnsureHpFillImageReference();
        EnsureBuffUIReference();
        CacheHPFillDefaultColor();
    }

    private void BindStateController()
    {
        if (stateController == null)
        {
            throw new System.InvalidOperationException($"[Monster] {gameObject.name}의 MonsterStateController가 인스펙터에 연결되지 않았습니다");
        }

        stateController.SetMonsterImageName(name);
    }

    private void EnsureHpSliderReference()
    {
        if (hpSlider != null)
        {
            return;
        }

        hpSlider = FindChildComponentByName<Slider>(transform, HealthBarObjectName);
        if (hpSlider == null)
        {
            hpSlider = CreateRuntimeHpSlider();
        }
    }

    private void EnsureHpFillImageReference()
    {
        if (hpFillImage != null)
        {
            return;
        }

        if (hpSlider != null && hpSlider.fillRect != null)
        {
            hpFillImage = hpSlider.fillRect.GetComponent<Image>();
        }

        if (hpFillImage == null && hpSlider != null)
        {
            hpFillImage = FindChildComponentByName<Image>(hpSlider.transform, HealthBarFillObjectName);
        }
    }

    private void EnsureBuffUIReference()
    {
        RectTransform buffRoot = EnsureBuffRoot();
        if (buffUI == null)
        {
            buffUI = GetComponentInChildren<BuffUI>(true);
        }

        if (buffUI == null && buffRoot != null)
        {
            buffUI = buffRoot.gameObject.AddComponent<BuffUI>();
        }

        if (buffUI != null)
        {
            buffUI.BindMonster(this, buffRoot);
        }
    }

    private RectTransform EnsureBuffRoot()
    {
        RectTransform foundBuffRoot = FindChildComponentByName<RectTransform>(transform, BuffRootObjectName);
        if (foundBuffRoot != null)
        {
            return foundBuffRoot;
        }

        Transform parentTransform = hpSlider != null ? hpSlider.transform : transform;
        GameObject buffObject = new GameObject(BuffRootObjectName, typeof(RectTransform));
        buffObject.layer = gameObject.layer;
        buffObject.transform.SetParent(parentTransform, false);

        RectTransform rectTransform = buffObject.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(160f, 90f);
            rectTransform.anchoredPosition = new Vector2(0f, -42f);
            rectTransform.localScale = Vector3.one;
        }

        return rectTransform;
    }

    private Slider CreateRuntimeHpSlider()
    {
        GameObject sliderObject = new GameObject(HealthBarObjectName, typeof(RectTransform), typeof(Slider));
        sliderObject.layer = gameObject.layer;
        sliderObject.transform.SetParent(transform, false);

        RectTransform sliderRect = sliderObject.transform as RectTransform;
        if (sliderRect != null)
        {
            sliderRect.anchorMin = new Vector2(0.5f, 0f);
            sliderRect.anchorMax = new Vector2(0.5f, 0f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(160f, 20f);
            sliderRect.anchoredPosition = new Vector2(0f, -10f);
            sliderRect.localScale = Vector3.one;
        }

        Image backgroundImage = CreateHealthBarImage(
            HealthBarBackgroundObjectName,
            sliderObject.transform,
            new Color(0f, 0f, 0f, 0.55f));
        RectTransform backgroundRect = backgroundImage != null ? backgroundImage.rectTransform : null;
        StretchHealthBarRect(backgroundRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform fillAreaRect = CreateHealthBarRect(HealthBarFillAreaObjectName, sliderObject.transform);
        StretchHealthBarRect(
            fillAreaRect,
            new Vector2(0f, 0.25f),
            new Vector2(1f, 0.75f),
            new Vector2(5f, 0f),
            new Vector2(-5f, 0f));

        Image fillImage = CreateHealthBarImage(HealthBarFillObjectName, fillAreaRect, Color.white);
        RectTransform fillRect = fillImage != null ? fillImage.rectTransform : null;
        StretchHealthBarRect(fillRect, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, 0f));

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;
        slider.targetGraphic = null;
        slider.fillRect = fillRect;

        hpFillImage = fillImage;
        return slider;
    }

    private static RectTransform CreateHealthBarRect(string objectName, Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.layer = parent.gameObject.layer;
        rectObject.transform.SetParent(parent, false);
        return rectObject.transform as RectTransform;
    }

    private static Image CreateHealthBarImage(string objectName, Transform parent, Color color)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void StretchHealthBarRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.localScale = Vector3.one;
    }

    private void CacheHPFillDefaultColor()
    {
        if (hasDefaultHPFillColor || hpFillImage == null)
        {
            return;
        }

        defaultHPFillColor = hpFillImage.color;
        hasDefaultHPFillColor = true;
    }

    private void UpdateHealthBarUI()
    {
        if (hpSlider != null)
        {
            int safeMaxHp = Mathf.Max(1, maxHP);
            hpSlider.minValue = 0f;
            hpSlider.maxValue = safeMaxHp;
            hpSlider.SetValueWithoutNotify(Mathf.Clamp(hp, 0, safeMaxHp));
        }

        if (hpFillImage == null)
        {
            return;
        }

        if (!hasDefaultHPFillColor)
        {
            CacheHPFillDefaultColor();
        }

        hpFillImage.color = defense > 0
            ? barrierHPFillColor
            : defaultHPFillColor;
    }

    private void UpdateBuffUI()
    {
        buffUI?.Refresh();
    }

    private static T FindChildComponentByName<T>(Transform root, string objectName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == objectName && child.TryGetComponent(out T component))
            {
                return component;
            }

            T foundComponent = FindChildComponentByName<T>(child, objectName);
            if (foundComponent != null)
            {
                return foundComponent;
            }
        }

        return null;
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

        maxHP = ScaleInfiniteMonsterValue(BaseMaxHp);
        hp = maxHP;
        defense = ScaleInfiniteMonsterValue(BaseDefense);
        attackPower = ScaleInfiniteMonsterValue(BaseAttackPower);
        name = GetType().Name;

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

        if (!CanDie())
        {
            hp = Mathf.Max(1, maxHP);
            defense = 0;
            ClearPlannedAction();
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

    protected int ScaleInfiniteMonsterValue(int value)
    {
        return InfiniteMode.ScaleMonsterValue(value);
    }

    private int ResolveMonsterBuffAmount(int buffId, int amount, bool isNonStackable)
    {
        if (amount <= 0)
        {
            return 0;
        }

        if (isNonStackable)
        {
            return 1;
        }

        return ShouldScaleMonsterBuffAmount(buffId)
            ? ScaleInfiniteMonsterValue(amount)
            : amount;
    }

    private static bool ShouldScaleMonsterBuffAmount(int buffId)
    {
        return BuffData.IsBeneficialBuffId(buffId)
            && buffId != BattleRuntimeDefinitions.ThiefBuffId;
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

    protected virtual bool CanDie()
    {
        return true;
    }

    protected abstract void BuildNextAction();
    protected abstract void ExecuteAction(PlayerData target);
}
