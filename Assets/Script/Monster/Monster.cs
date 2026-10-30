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
    public Slider hpSlider;
    public Image hpFillImage;
    // 체력 바 중앙 수치와 좌측 보호막 표시
    public HealthBarView healthBarView;
    public BuffUI buffUI;
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
    private int? plannedProtectionIntentValue;
    private string plannedIntentDescription = string.Empty;
    // 기절 의도 표시 중 (언어 변경 시 기절 문구로 다시 만듦)
    private bool showingStunIntent;
    private readonly List<MonsterIntentIconType> plannedIntentIcons = new();
    // 의도 툴팁용 행동 종류별 문장 (비어 있으면 의도 설명 전체를 한 줄로 표시)
    private readonly List<MonsterIntentLine> plannedIntentLines = new();
    private IntentTooltipSource intentTooltipSource;
    private int plannedPatternId = 0;
    private bool skipCurrentTurnAction = false;
    private bool hasTriggeredDeath = false;
    private bool hasLeftCombat = false;
    // 자폭 사망 처리 중 (처치로 세지 않음)
    private bool isSelfDestructing = false;
    private bool hasInitializedBattleStart = false;
    private bool isSelectionHighlighted = false;
    private int lastSelectionClickFrame = -1;
    private Color defaultHPFillColor = Color.white;
    private bool hasDefaultHPFillColor = false;
    private readonly Dictionary<Graphic, Color> originalGraphicColors = new();
    private readonly Dictionary<SpriteRenderer, Color> originalSpriteColors = new();
    // 중첩이 0이 되어도 제거하지 않고 표시하는 시작 패시브 (거울 0으로 시작)
    private readonly HashSet<int> zeroStackHeldBuffIds = new();

    public bool HasAttackIntent => hasAttackIntent;
    // 이번 턴 행동을 쉬는 기절 상태
    public bool IsStunned => skipCurrentTurnAction && !IsDead();
    public int PlannedIntentValue => plannedIntentValue;
    public int PlannedPatternId => plannedPatternId;
    public IReadOnlyList<MonsterIntentIconType> PlannedIntentIcons => plannedIntentIcons;
    public IReadOnlyList<MonsterIntentLine> PlannedIntentLines => plannedIntentLines;
    public MonsterStateType CurrentState => stateController != null
        ? stateController.CurrentState
        : throw new System.InvalidOperationException($"[Monster] {gameObject.name}의 MonsterStateController가 인스펙터에 연결되지 않았습니다");
    public abstract int MonsterId { get; }
    protected virtual string MonsterName => GetType().Name;
    // 표시용 이름 (데이터 몬스터는 계층 포함 이름)
    public string DisplayName => MonsterName;
    protected abstract int BaseMaxHp { get; }
    protected virtual int BaseAttackPower => 0;
    protected virtual int BaseDefense => 0;
    protected virtual bool IsBossMonster => false;
    public bool IsBoss => IsBossMonster;
    private int permanentDefense;
    // 이번 내 턴 동안 잃은 체력 (내 턴 시작 시 초기화)
    public int HpLostThisTurn { get; private set; }
    // 합체 등으로 커진 패턴 배수 (공격 피해·보호막 획득에 곱함)
    public int patternMultiplier = 1;

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

        if (TrainingBattleManager.Instance != null)
        {
            TrainingBattleManager.Instance.RegisterMonster(this);
        }

        EnsureBattleStartInitialized();
        UpdateUI();
    }

    protected virtual void OnDisable()
    {
        SetSelectionHighlight(false);
        ReleaseHealthBarUI();
        buffUI?.UnbindMonster(this);
    }

    protected virtual void OnDestroy()
    {
        ReleaseHealthBarUI();
        buffUI?.UnbindMonster(this);
        SetSelectionHighlight(false);

        if (TrainingBattleManager.Instance != null)
        {
            TrainingBattleManager.Instance.UnregisterMonster(this);
        }
    }

    public void UpdateUI()
    {
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
            OnTurnActionSkipped();
            ClearPlannedAction();
            UpdateUI();
            return;
        }

        bool isAttackAction = hasAttackIntent;
        if (isAttackAction)
        {
            stateController.EnterAttack();
            TrainingBattleManager.Instance?.HandleMonsterAttackActionStarted(this);
            // 덫 등 공격 직전 효과로 사망했다면 행동하지 않음
            if (IsDead())
            {
                TrainingBattleManager.Instance?.HandleMonsterAttackActionEnded(this);
                return;
            }
        }

        ExecuteAction(target);
        if (isAttackAction) TrainingBattleManager.Instance?.HandleMonsterAttackActionEnded(this);

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

        int actualHpLoss = Mathf.Min(Mathf.Max(0, hp), damageAfterDefense);
        hp -= damageAfterDefense;
        SetDefenseValue(defenseBeforeHit - finalDamage);

        Debug.Log($"{name} took {damageAfterDefense} damage. (HP: {hp}/{maxHP})");
        if (finalDamage > 0)
        {
            stateController.EnterHit();
        }

        if (damageAfterDefense > 0)
        {
            HpLostThisTurn += actualHpLoss;
            TrainingBattleManager.Instance?.HandleMonsterHpLost(this, actualHpLoss);
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
        HpLostThisTurn += lostAmount;
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

    // 자폭: 사망 시 효과(화염 전이 등)는 발동하지만 처치가 아니므로 처치 효과(처형자 등)는 발동하지 않음
    // 피해가 아니므로 체력 손실 효과도 발동하지 않음
    public void SelfDestruct()
    {
        if (hasTriggeredDeath || hasLeftCombat)
        {
            return;
        }

        hp = 0;
        defense = 0;
        isSelfDestructing = true;
        try
        {
            HandleDeathIfNeeded();
        }
        finally
        {
            isSelfDestructing = false;
        }

        UpdateUI();
        Debug.Log($"[Monster] {name} 자폭");
    }

    public void LeaveCombat()
    {
        if (hasTriggeredDeath || hasLeftCombat)
        {
            return;
        }

        hasLeftCombat = true;
        SetSelectionHighlight(false);
        ReleaseHealthBarUI();
        buffUI?.UnbindMonster(this);
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

    public void ResetTurnHpLoss()
    {
        HpLostThisTurn = 0;
    }

    public void AddDefense(int amount)
    {
        int finalAmount = Mathf.Max(0, amount) * Mathf.Max(1, patternMultiplier);
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager != null)
        {
            finalAmount = battleManager.ResolveMonsterBarrierGain(this, finalAmount);
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

    // 0 중첩으로 보유를 시작하고, 이후 소모로 0이 되어도 효과를 유지
    public void HoldBuffAtZero(int buffId)
    {
        if (buffId <= 0)
        {
            return;
        }

        zeroStackHeldBuffIds.Add(buffId);
        if (!currentBuffs.Exists(b => b.data != null && b.data.buffId == buffId))
        {
            currentBuffs.Add(new Buff(BuffMetadataResolver.Resolve(buffId), 0));
        }

        UpdateUI();
    }

    public void AddBuff(int buffId, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        // 힘처럼 음수 중첩을 허용하는 효과는 부호 그대로 더함
        if (BuffData.AllowsNegativeStack(buffId) && (amount < 0 || GetBuffStack(buffId) < 0))
        {
            AddSignedBuffStack(buffId, amount);
            return;
        }

        if (amount < 0)
        {
            // 중첩 불가 효과에 음수를 부여하면 효과 제거 (E-26)
            if (IsNonStackableBuff(buffId))
            {
                RemoveBuff(buffId);
                UpdateUI();
            }
            return;
        }

        bool isNonStackable = IsNonStackableBuff(buffId);
        int resolvedAmount = ResolveMonsterBuffAmount(amount, isNonStackable);
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

    // 음수 중첩 허용 효과의 중첩 변경 (0이 되면 제거)
    private void AddSignedBuffStack(int buffId, int amount)
    {
        Buff existingBuff = currentBuffs.Find(b =>
            b.data != null &&
            b.data.buffId == buffId);
        int nextStack = (existingBuff != null ? existingBuff.stack : 0) + amount;
        if (nextStack == 0)
        {
            if (existingBuff != null)
            {
                currentBuffs.Remove(existingBuff);
            }
        }
        else if (existingBuff != null)
        {
            existingBuff.data = BuffMetadataResolver.Resolve(buffId);
            existingBuff.stack = nextStack;
        }
        else
        {
            currentBuffs.Add(new Buff(BuffMetadataResolver.Resolve(buffId), nextStack));
        }

        UpdateUI();
    }

    // 몬스터 턴은 모든 몬스터의 보호막 제거를 먼저 끝낸 뒤 턴 시작 효과를 처리
    public void RemoveTurnStartBarrier()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        int retainedDefense = Mathf.Clamp(permanentDefense, 0, defense);
        if (defense > retainedDefense && !(battleManager?.ShouldKeepMonsterBarrierOnTurnStart(this) ?? false))
        {
            SetDefenseValue(retainedDefense);
            Debug.Log($"[Monster] {name} 턴 시작으로 보호막이 제거됩니다.");
        }
        UpdateUI();
    }

    public void OnTurnStart()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
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
        int finalAmount = Mathf.Max(0, amount);
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
        ApplyStunIntentText();
        SetPlannedPattern(0, MonsterIntentIconType.Stun);
        UpdateIntentUI();
    }

    // 언어 변경 시 표시 중인 의도 문구를 현재 언어로 다시 만듦
    public void RefreshIntentText()
    {
        if (showingStunIntent)
        {
            ApplyStunIntentText();
            UpdateIntentUI();
            return;
        }

        OnRefreshIntentText();
    }

    protected virtual void OnRefreshIntentText()
    {
    }

    private void ApplyStunIntentText()
    {
        SetIntent(MonsterIntentText.Line("intent.plain.stunned"));
        SetIntentLines(new[] { new MonsterIntentLine(MonsterIntentText.Line("intent.line.stunned"), MonsterIntentIconType.Stun) });
        showingStunIntent = true;
    }

    public virtual void ScalePlannedIntent(float multiplier)
    {
        if (multiplier < 0f || !hasAttackIntent || plannedIntentValue <= 0)
        {
            return;
        }

        int previousValue = plannedIntentValue;
        plannedIntentValue = Mathf.Max(0, Mathf.FloorToInt(plannedIntentValue * multiplier));
        plannedIntentDescription = ReplaceIntentValue(plannedIntentDescription, previousValue, plannedIntentValue);
        foreach (MonsterIntentLine line in plannedIntentLines)
        {
            if (line.Contains(MonsterIntentIconType.Attack) || line.Contains(MonsterIntentIconType.Bomb))
            {
                line.ReplaceSentence(ReplaceIntentValue(line.Sentence, previousValue, plannedIntentValue));
                break;
            }
        }
        UpdateUI();
    }

    protected void SetAttackIntent(int intentValue, string intentDescription)
    {
        showingStunIntent = false;
        hasAttackIntent = true;
        plannedIntentValue = Mathf.Max(0, intentValue);
        plannedProtectionIntentValue = null;
        plannedIntentDescription = intentDescription;
        plannedIntentLines.Clear();
    }

    protected void SetIntent(string intentDescription)
    {
        showingStunIntent = false;
        hasAttackIntent = false;
        plannedIntentValue = 0;
        plannedProtectionIntentValue = null;
        plannedIntentDescription = intentDescription;
        plannedIntentLines.Clear();
    }

    // SetIntent · SetAttackIntent 뒤에 호출 (의도 설정 시 이전 문장은 지워짐)
    protected void SetIntentLines(IEnumerable<MonsterIntentLine> lines)
    {
        plannedIntentLines.Clear();
        if (lines == null)
        {
            return;
        }

        foreach (MonsterIntentLine line in lines)
        {
            if (line != null)
            {
                plannedIntentLines.Add(line);
            }
        }
    }

    // 의도 영역 호버 시 표시할 문구
    public string GetIntentTooltipText()
    {
        if (!isActiveAndEnabled || IsDead() || hasLeftCombat || plannedIntentIcons.Count == 0)
        {
            return string.Empty;
        }

        if (plannedIntentLines.Count > 0)
        {
            return IntentTooltipTextBuilder.Build(plannedIntentLines);
        }

        if (string.IsNullOrWhiteSpace(plannedIntentDescription))
        {
            return string.Empty;
        }

        return IntentTooltipTextBuilder.Build(new[] { new MonsterIntentLine(plannedIntentDescription, plannedIntentIcons.ToArray()) });
    }

    protected void SetProtectionIntentValue(int intentValue)
    {
        plannedProtectionIntentValue = Mathf.Max(0, intentValue);
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
        return UnityEngine.Mathf.Max(0, ApplyOutgoingDamageModifier(baseDamage));
    }

    protected int PreviewBarrierGain(int baseAmount)
    {
        int safeAmount = Mathf.Max(0, baseAmount) * Mathf.Max(1, patternMultiplier);
        int resolvedAmount = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ResolveMonsterBarrierGain(this, safeAmount)
            : safeAmount;
        return UnityEngine.Mathf.Max(0, resolvedAmount);
    }

    protected int PreviewMonsterBuffAmount(int buffId, int amount)
    {
        return ResolveMonsterBuffAmount(amount, IsNonStackableBuff(buffId));
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

        return Mathf.Max(0, amount);
    }

    protected void AddDebuffToPlayer(PlayerData target, int buffId, int amount)
    {
        target?.AddBuff(buffId, amount);
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
        if (stateController == null)
        {
            return;
        }

        stateController.UpdateIntendIcons(isActiveAndEnabled && !IsDead() && !hasLeftCombat
            ? plannedIntentIcons
            : null, hasAttackIntent ? plannedIntentValue : (int?)null, plannedProtectionIntentValue);
    }

    private void BindStateController()
    {
        if (stateController == null)
        {
            throw new System.InvalidOperationException($"[Monster] {gameObject.name}의 MonsterStateController가 인스펙터에 연결되지 않았습니다");
        }

        stateController.SetMonsterImageName(name);
        intentTooltipSource ??= new IntentTooltipSource(this);
        stateController.BindIntentTooltip(intentTooltipSource);
    }

    private sealed class IntentTooltipSource : ITooltipSource
    {
        private readonly Monster owner;

        public IntentTooltipSource(Monster owner)
        {
            this.owner = owner;
        }

        public string GetTooltipText()
        {
            return owner != null ? owner.GetIntentTooltipText() : string.Empty;
        }
    }

    private void ReleaseHealthBarUI()
    {
        // 이전 몬스터의 늦은 제거 처리가 새 몬스터의 체력바를 숨기지 않도록 소유자를 확인
        // 같은 스폰 칸을 재사용할 때 이전 몬스터의 체력바 색상과 연결을 먼저 정리
        // 재사용 전에 이전 컴포넌트의 UI 참조를 해제하므로 소유자 목록은 사용하지 않음
        if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(false);
        }

        if (hpFillImage != null && hasDefaultHPFillColor)
        {
            hpFillImage.color = defaultHPFillColor;
        }
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
        if (!isActiveAndEnabled || IsDead() || hasLeftCombat)
        {
            ReleaseHealthBarUI();
            return;
        }

        if (hpSlider == null)
        {
            return;
        }

        hpSlider.gameObject.SetActive(true);
        int safeMaxHp = Mathf.Max(1, maxHP);
        hpSlider.minValue = 0f;
        hpSlider.maxValue = safeMaxHp;
        hpSlider.SetValueWithoutNotify(Mathf.Clamp(hp, 0, safeMaxHp));
        healthBarView?.Refresh(hp, maxHP, defense);

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
        if (!isActiveAndEnabled || IsDead() || hasLeftCombat)
        {
            buffUI?.UnbindMonster(this);
            return;
        }

        buffUI?.BindMonster(this);
        buffUI?.Refresh();
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
            if (zeroStackHeldBuffIds.Contains(buffId))
            {
                buff.stack = 0;
                return;
            }

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
        zeroStackHeldBuffIds.Clear();

        maxHP = UnityEngine.Mathf.Max(0, BaseMaxHp);
        hp = maxHP;
        defense = UnityEngine.Mathf.Max(0, BaseDefense);
        permanentDefense = 0;
        patternMultiplier = 1;
        HpLostThisTurn = 0;
        attackPower = UnityEngine.Mathf.Max(0, BaseAttackPower);
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
        SetSelectionHighlight(false);
        defense = 0;
        ClearPlannedAction();
        OnDeathTriggered();
        TrainingBattleManager.Instance?.HandleMonsterDeath(this, !isSelfDestructing);
        Debug.Log($"[Monster] {name} 처치");
    }

    private void ClearPlannedAction()
    {
        hasAttackIntent = false;
        plannedIntentValue = 0;
        plannedProtectionIntentValue = null;
        plannedIntentDescription = string.Empty;
        plannedPatternId = 0;
        plannedIntentIcons.Clear();
        plannedIntentLines.Clear();
        UpdateIntentUI();
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
        permanentDefense = Mathf.Min(permanentDefense, defense);
        TrainingBattleManager.Instance?.HandleMonsterDefenseChanged(this, previousDefense, defense);
    }

    public void AddPermanentDefense(int amount)
    {
        int previousDefense = defense;
        AddDefense(amount);
        permanentDefense += Mathf.Max(0, defense - previousDefense);
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
        if (baseDamage < 0)
        {
            return 0;
        }

        int scaledDamage = baseDamage * Mathf.Max(1, patternMultiplier);
        return TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ModifyMonsterOutgoingDamage(this, scaledDamage)
            : Mathf.Max(0, scaledDamage);
    }

    private int ResolveMonsterBuffAmount(int amount, bool isNonStackable)
    {
        if (amount <= 0)
        {
            return 0;
        }

        if (isNonStackable)
        {
            return 1;
        }

        return amount;
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

    // 기절로 이번 턴 행동을 쉬었을 때
    protected virtual void OnTurnActionSkipped()
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
