using System.Collections.Generic;
using System.Text;
using UnityEngine;

// v1.0 데이터 기반 몬스터: monsters.json의 체력·시작 효과·패턴·AI를 사용
// 종 클래스는 특수 패턴(special)과 고유 동작만 구현
public abstract class DataMonster : Monster, IMonsterAIContext
{
    // AddComponent 직후 Awake에서 읽을 정의와 엇갈림 순번 (MonsterPrefabFactory가 설정)
    public static MonsterDefinition PendingDefinition;
    public static int PendingStaggerIndex;

    private MonsterDefinition definition;
    private MonsterAI ai;
    private MonsterIntentQueue intentQueue;
    private int staggerIndex;

    private string currentKey;
    private bool needsDecision = true;
    // 조건 규칙으로 막 결정한 의도 (조건 변화 시 즉시 재판정 대상)
    private bool currentFromRules;
    // 카드로 바뀐 의도는 이번 턴 재판정하지 않음
    private bool currentChangedByCard;
    private float intentDamageScale = 1f;
    private int lastUnblockedDamage;
    // 현재 패턴에서 공격을 실행했는지 (공격 후 이탈 = 자폭)
    private bool hasAttackedInCurrentPattern;
    private readonly Dictionary<string, int> patternUseCounts = new Dictionary<string, int>();
    private readonly Dictionary<MonsterActionDefinition, int> plannedHits = new Dictionary<MonsterActionDefinition, int>();

    public MonsterDefinition Definition => definition;
    public string CurrentPatternKey => needsDecision ? null : currentKey;
    public IReadOnlyList<string> QueuedPatternKeys => intentQueue != null ? intentQueue.Items : new List<string>();
    public override int MonsterId => definition != null ? definition.id : 0;
    public int Species => definition != null ? definition.species : 0;
    protected override string MonsterName => definition != null ? MonsterIntentText.MonsterName(definition) : base.MonsterName;
    protected override int BaseMaxHp => definition != null ? definition.hp : 1;
    protected override bool IsBossMonster => definition != null && definition.IsBoss;

    protected override void Awake()
    {
        definition = PendingDefinition;
        staggerIndex = Mathf.Max(0, PendingStaggerIndex);
        PendingDefinition = null;
        PendingStaggerIndex = 0;
        if (definition == null)
        {
            Debug.LogError($"[DataMonster] {GetType().Name}의 몬스터 정의가 없습니다. MonsterPrefabFactory로 생성해야 합니다");
        }

        base.Awake();
    }

    protected override void OnBattleStart()
    {
        ResetAI();
        ApplyStartEffects();
    }

    protected override void OnRevivedTriggered()
    {
        // E-42 부활 시 최대 체력·상태 초기화 후 시작 효과 다시 적용
        ResetAI();
        ApplyStartEffects();
    }

    private void ResetAI()
    {
        List<string> keys = new List<string>();
        if (definition?.patterns != null)
        {
            foreach (MonsterPatternDefinition pattern in definition.patterns)
            {
                if (pattern != null)
                {
                    keys.Add(pattern.key);
                }
            }
        }

        ai = new MonsterAI(definition?.ai, keys);
        intentQueue = new MonsterIntentQueue(() => ai.NextPattern(this));
        if (GameRules.Combat.patternStagger)
        {
            ai.ApplyStagger(staggerIndex);
        }

        currentKey = null;
        needsDecision = true;
        currentFromRules = false;
        currentChangedByCard = false;
        intentDamageScale = 1f;
        patternUseCounts.Clear();
        plannedHits.Clear();
    }

    private void ApplyStartEffects()
    {
        if (definition == null)
        {
            return;
        }

        foreach (MonsterStartEffect effect in definition.start ?? new List<MonsterStartEffect>())
        {
            if (effect == null || effect.buffId <= 0)
            {
                continue;
            }

            // 0으로 시작하는 패시브는 중첩 없이 보유만 함 (거울)
            if (effect.amount == 0)
            {
                HoldBuffAtZero(effect.buffId);
            }
            else
            {
                AddBuff(effect.buffId, effect.amount);
            }
        }

        if (definition.startBlock > 0)
        {
            AddDefense(definition.startBlock);
        }
    }

    // ── 의도 결정 ─────────────────────────────────────────────────────
    protected override void BuildNextAction()
    {
        // 턴 종료 결정형은 OnTurnEnded에서 정하며 첫 의도와 기절 후 의도만 여기서 정함
        if (needsDecision)
        {
            DecideCurrentIntent();
        }

        RenderIntent();
    }

    private void DecideCurrentIntent()
    {
        string overrideKey = ai.CheckOverride(this);
        if (!string.IsNullOrEmpty(overrideKey))
        {
            currentKey = overrideKey;
            currentFromRules = false;
        }
        else
        {
            currentKey = intentQueue.Dequeue(out bool generated);
            currentFromRules = generated && ai.IsRules;
        }

        // 소환할 자리가 없으면 소환 패턴을 건너뛰고 다음 패턴으로 (같은 턴 다른 몬스터의 소환 예약 포함)
        for (int guard = 0; guard < definition.patterns.Count && IsSummonPattern(currentKey) && !TryReserveSummon(); guard++)
        {
            currentKey = intentQueue.Dequeue(out bool skippedGenerated);
            currentFromRules = skippedGenerated && ai.IsRules;
        }

        needsDecision = false;
        currentChangedByCard = false;
        plannedHits.Clear();
    }

    protected override void OnTurnEnded()
    {
        if (IsDead())
        {
            return;
        }

        // lock 조건이 턴 종료 시 충족되면 고정 순환으로 즉시 전환
        if (ai.LockDecidesAtTurnEnd && ai.TryActivateLock(this))
        {
            intentQueue.Clear();
            currentKey = ai.NextPattern(this);
            currentFromRules = false;
            needsDecision = false;
            currentChangedByCard = false;
            plannedHits.Clear();
            return;
        }

        if (needsDecision && ai.DecideAtTurnEnd)
        {
            DecideCurrentIntent();
        }
    }

    protected override void OnTurnActionSkipped()
    {
        // R-71 기절은 그 턴의 의도를 소화한 것으로 처리
        if (GameRules.Combat.stunConsumesIntent)
        {
            needsDecision = true;
        }
    }

    // E-40 조건 규칙형은 몬스터 사망·보호막 변화 등으로 조건이 바뀌면 즉시 의도 수정
    public void ReevaluateConditionalIntent()
    {
        if (IsDead() || needsDecision || currentChangedByCard || IsStunned || ai == null)
        {
            return;
        }

        string overrideKey = ai.CheckOverride(this);
        if (!string.IsNullOrEmpty(overrideKey) && overrideKey != currentKey)
        {
            currentKey = overrideKey;
            currentFromRules = false;
            plannedHits.Clear();
            RenderIntent();
            return;
        }

        if (currentFromRules && !ai.DecideAtTurnEnd && ai.TryReevaluateRules(this, currentKey, out string newKey) && newKey != currentKey)
        {
            currentKey = newKey;
            plannedHits.Clear();
            RenderIntent();
        }
    }

    // ── 카드에 의한 의도 변경 (R-70, 2-8절) ──────────────────────────────
    public bool CanChangeIntent
    {
        get
        {
            if (IsDead() || IsStunned || needsDecision || definition == null || definition.patterns.Count <= 1)
            {
                return false;
            }

            // 이탈 의도 · 이번 턴 봉인된 상태는 변경 불가
            if (currentKey == MonsterIntentQueue.NoActionKey)
            {
                return false;
            }

            MonsterPatternDefinition pattern = definition.FindPattern(currentKey);
            return pattern == null || !ContainsAction(pattern.actions, MonsterActionDefinition.Leave);
        }
    }

    // n칸 미루기
    public bool DelayIntent(int count)
    {
        if (!CanChangeIntent)
        {
            return false;
        }

        currentKey = intentQueue.Delay(currentKey, count);
        MarkChangedByCard();
        return true;
    }

    // 공격 의도 복사 (도발): 큐에서 가장 빠른 공격 의도를 현재 의도로
    public bool CopyEarliestAttackIntent()
    {
        if (!CanChangeIntent)
        {
            return false;
        }

        string attackKey = intentQueue.FindFirstAttack(IsAttackPattern);
        if (string.IsNullOrEmpty(attackKey))
        {
            return false;
        }

        currentKey = attackKey;
        MarkChangedByCard();
        return true;
    }

    // 봉인: 큐 맨 앞에 행동 없음 1칸, 보스는 대신 피해 15
    public bool SealNextAction()
    {
        if (IsDead())
        {
            return false;
        }

        if (IsBoss)
        {
            TrainingBattleManager battleManager = TrainingBattleManager.Instance;
            int damage = battleManager != null ? battleManager.ResolvePlayerEffectDamage(15) : 15;
            TakeDamage(damage, 0);
            return true;
        }

        if (!CanChangeIntent)
        {
            return false;
        }

        intentQueue.InsertNoAction();
        return true;
    }

    private void MarkChangedByCard()
    {
        currentFromRules = false;
        currentChangedByCard = true;
        plannedHits.Clear();
        RenderIntent();
        UpdateUI();
    }

    public override void ScalePlannedIntent(float multiplier)
    {
        if (multiplier < 0f || !HasAttackIntent)
        {
            return;
        }

        intentDamageScale *= multiplier;
        RenderIntent();
        UpdateUI();
    }

    // 언어 변경 시 의도 문구 다시 만들기
    protected override void OnRefreshIntentText()
    {
        RefreshIntent();
    }

    // 의도 표시 갱신 (거울 중첩 등 미리보기 값이 바뀔 때)
    public void RefreshIntent()
    {
        if (!needsDecision)
        {
            RenderIntent();
            UpdateUI();
        }
    }

    // ── 의도 표시 ─────────────────────────────────────────────────────
    private void RenderIntent()
    {
        if (string.IsNullOrEmpty(currentKey) || currentKey == MonsterIntentQueue.NoActionKey)
        {
            SetIntent(MonsterIntentText.Line("intent.line.nothing"));
            SetPlannedPattern(0, MonsterIntentIconType.Stun);
            return;
        }

        MonsterPatternDefinition pattern = definition.FindPattern(currentKey);
        if (pattern == null)
        {
            SetIntent(string.Empty);
            SetPlannedPattern(0);
            return;
        }

        List<MonsterActionDefinition> actions = FlattenActions(pattern.actions);
        StringBuilder text = new StringBuilder();
        List<MonsterIntentIconType> icons = new List<MonsterIntentIconType>();
        // 의도 툴팁용 행동 종류별 문장 (버프는 [buff:id] 표기, 툴팁에서 현재 언어 이름 · 극성 색)
        List<MonsterIntentLine> lines = new List<MonsterIntentLine>();
        int? firstAttackDamage = null;
        int protection = 0;
        bool hasProtection = false;
        bool hasSpecial = false;

        foreach (MonsterActionDefinition action in actions)
        {
            switch (action.type)
            {
                case MonsterActionDefinition.Attack:
                    int damage = PreviewAttackDamage(action.amount);
                    int hits = ResolvePlannedHits(action);
                    firstAttackDamage ??= damage;
                    string attackSentence = MonsterIntentText.Line("intent.line.attack", new Dictionary<string, object> { { "damage", damage }, { "hits", hits } });
                    AppendSentence(text, attackSentence);
                    AddIcon(icons, MonsterIntentIconType.Attack);
                    AddIntentLine(lines, MonsterIntentIconType.Attack, attackSentence);
                    break;
                case MonsterActionDefinition.Block:
                    int block = PreviewBlock(action);
                    protection += block;
                    hasProtection = true;
                    string blockSentence = BuildBlockText(action, block);
                    AppendSentence(text, blockSentence);
                    AddIcon(icons, MonsterIntentIconType.Protection);
                    AddIntentLine(lines, MonsterIntentIconType.Protection, blockSentence);
                    break;
                case MonsterActionDefinition.Buff:
                    int gain = PreviewSelfBuffGain(action);
                    string buffSentence = MonsterIntentText.Line(
                        action.target == MonsterActionDefinition.TargetAllies ? "intent.line.buffAllies" : "intent.line.buff",
                        BuffArguments(action.buffId, gain));
                    AppendSentence(text, MonsterIntentText.Plain(buffSentence));
                    AddIcon(icons, MonsterIntentIconType.BeneficialEffect);
                    AddIntentLine(lines, MonsterIntentIconType.BeneficialEffect, buffSentence);
                    break;
                case MonsterActionDefinition.Debuff:
                    string debuffSentence = MonsterIntentText.Line("intent.line.debuff", BuffArguments(action.buffId, PreviewPlayerDebuffAmount(action.buffId, action.amount)));
                    AppendSentence(text, MonsterIntentText.Plain(debuffSentence));
                    AddIcon(icons, MonsterIntentIconType.HarmfulEffect);
                    AddIntentLine(lines, MonsterIntentIconType.HarmfulEffect, debuffSentence);
                    break;
                case MonsterActionDefinition.ReducePlayerBuff:
                    string reduceSentence = MonsterIntentText.Line("intent.line.reducePlayerBuff", BuffArguments(action.buffId, action.amount));
                    AppendSentence(text, MonsterIntentText.Plain(reduceSentence));
                    AddIcon(icons, MonsterIntentIconType.HarmfulEffect);
                    AddIntentLine(lines, MonsterIntentIconType.HarmfulEffect, reduceSentence);
                    break;
                case MonsterActionDefinition.Heal:
                    string healSentence = MonsterIntentText.Line("intent.line.heal", new Dictionary<string, object> { { "amount", Mathf.Max(0, action.amount) } });
                    AppendSentence(text, healSentence);
                    AddIcon(icons, MonsterIntentIconType.Heal);
                    AddIntentLine(lines, MonsterIntentIconType.Heal, healSentence);
                    break;
                case MonsterActionDefinition.HealUnblocked:
                    string healUnblockedSentence = MonsterIntentText.Line("intent.line.healUnblocked");
                    AppendSentence(text, healUnblockedSentence);
                    AddIcon(icons, MonsterIntentIconType.Heal);
                    AddIntentLine(lines, MonsterIntentIconType.Heal, healUnblockedSentence);
                    break;
                case MonsterActionDefinition.AddCard:
                    string cardSentence = BuildCardText(action);
                    AppendSentence(text, cardSentence);
                    AddIcon(icons, MonsterIntentIconType.DisruptCard);
                    AddIntentLine(lines, MonsterIntentIconType.DisruptCard, cardSentence);
                    break;
                case MonsterActionDefinition.Summon:
                    MonsterDefinition summoned = MonsterDatabase.Get(definition.GetSpeciesIdInSameTier(action.species));
                    string summonedName = summoned != null ? MonsterIntentText.MonsterName(summoned) : MonsterIntentText.Line("intent.word.monster");
                    string summonSentence = MonsterIntentText.Line("intent.line.summon", new Dictionary<string, object> { { "monster", summonedName } });
                    AppendSentence(text, summonSentence);
                    AddIcon(icons, MonsterIntentIconType.Summon);
                    AddIntentLine(lines, MonsterIntentIconType.Summon, summonSentence);
                    break;
                case MonsterActionDefinition.Leave:
                    string leaveSentence = MonsterIntentText.Line("intent.line.leave");
                    AppendSentence(text, leaveSentence);
                    // 공격 후 이탈은 자폭 아이콘
                    if (icons.Remove(MonsterIntentIconType.Attack))
                    {
                        icons.Insert(0, MonsterIntentIconType.Bomb);
                        // 툴팁도 공격 줄을 자폭 줄로 바꾸고 사망 문장을 이어 붙임
                        // 자폭은 처치가 아닌 사망으로 처리 (사망 시 효과만 발동, 사용자 확인)
                        foreach (MonsterIntentLine line in lines)
                        {
                            line.ReplaceIconType(MonsterIntentIconType.Attack, MonsterIntentIconType.Bomb);
                        }

                        AddIntentLine(lines, MonsterIntentIconType.Bomb, MonsterIntentText.Line("intent.line.death"));
                    }
                    else
                    {
                        AddIcon(icons, MonsterIntentIconType.Leave);
                        AddIntentLine(lines, MonsterIntentIconType.Leave, leaveSentence);
                    }
                    break;
                case MonsterActionDefinition.Nothing:
                    string nothingSentence = MonsterIntentText.Line("intent.line.nothing");
                    AppendSentence(text, nothingSentence);
                    AddIcon(icons, MonsterIntentIconType.Stun);
                    AddIntentLine(lines, MonsterIntentIconType.Stun, nothingSentence);
                    break;
                case MonsterActionDefinition.Special:
                    hasSpecial = true;
                    foreach (MonsterIntentIconType icon in GetSpecialIcons(action.key))
                    {
                        AddIcon(icons, icon);
                    }
                    break;
            }
        }

        // 특수 패턴 문구는 현재 언어 표에서 (툴팁 줄은 참조 토큰 유지, 설명은 이름으로)
        string patternText = hasSpecial ? MonsterIntentText.PatternText(pattern) : string.Empty;
        string description = hasSpecial && !string.IsNullOrEmpty(patternText) ? MonsterIntentText.Plain(patternText) : text.ToString();
        if (firstAttackDamage.HasValue)
        {
            SetAttackIntent(firstAttackDamage.Value, description);
        }
        else
        {
            SetIntent(description);
        }

        SetPlannedPattern(pattern.id, icons.ToArray());
        // 특수 패턴은 패턴 문구 전체를 한 줄로, 의도 이름은 아이콘 종류를 이어서 표시
        SetIntentLines(hasSpecial && !string.IsNullOrEmpty(patternText)
            ? new[] { new MonsterIntentLine(patternText, icons.ToArray()) }
            : lines);
        if (hasProtection)
        {
            SetProtectionIntentValue(protection);
        }
    }

    protected virtual IEnumerable<MonsterIntentIconType> GetSpecialIcons(string key)
    {
        yield return MonsterIntentIconType.HarmfulEffect;
    }

    private string BuildBlockText(MonsterActionDefinition action, int block)
    {
        switch (action.target)
        {
            case MonsterActionDefinition.TargetAllies:
                return MonsterIntentText.Line("intent.line.blockAllies", new Dictionary<string, object> { { "amount", block } });
            case MonsterActionDefinition.TargetSpecies:
                Monster target = FindSpeciesAlly(action.species);
                return target != null && target != this
                    ? MonsterIntentText.Line("intent.line.blockTarget", new Dictionary<string, object> { { "target", target.DisplayName }, { "amount", block } })
                    : MonsterIntentText.Line("intent.line.block", new Dictionary<string, object> { { "amount", block } });
            default:
                return MonsterIntentText.Line("intent.line.block", new Dictionary<string, object> { { "amount", block } });
        }
    }

    private string BuildCardText(MonsterActionDefinition action)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        int resolvedCardId = battleManager != null ? battleManager.ResolvePersistentUpgradeCardId(action.cardId) : action.cardId;
        Card card = CardManager.GetCardAsCard(resolvedCardId) ?? CardManager.GetCardAsCard(action.cardId);
        string cardName = card != null ? card.cardName : action.cardId.ToString();
        // 손 · 뽑을 카드 더미 · 버린 카드 더미마다 문장을 따로 둠 (언어마다 어순이 다름)
        string key = action.zone == "hand" ? "intent.line.addCard.hand" : action.zone == "drawShuffle" ? "intent.line.addCard.drawShuffle" : "intent.line.addCard.discard";
        return MonsterIntentText.Line(key, new Dictionary<string, object> { { "card", cardName }, { "count", Mathf.Max(1, action.count) } });
    }

    private static void AppendSentence(StringBuilder text, string sentence)
    {
        if (text.Length > 0)
        {
            text.Append(' ');
        }

        text.Append(sentence);
    }

    // 같은 종류의 행동은 한 줄에 이어 붙임
    private static void AddIntentLine(List<MonsterIntentLine> lines, MonsterIntentIconType icon, string sentence)
    {
        foreach (MonsterIntentLine line in lines)
        {
            if (line.IsSingle(icon))
            {
                line.AppendSentence(sentence);
                return;
            }
        }

        lines.Add(new MonsterIntentLine(sentence, icon));
    }

    private static void AddIcon(List<MonsterIntentIconType> icons, MonsterIntentIconType icon)
    {
        if (icon != MonsterIntentIconType.None && !icons.Contains(icon))
        {
            icons.Add(icon);
        }
    }

    private int PreviewAttackDamage(int amount)
    {
        return PreviewOutgoingDamage(Mathf.FloorToInt(Mathf.Max(0, amount) * intentDamageScale));
    }

    private int PreviewBlock(MonsterActionDefinition action)
    {
        if (action.target == MonsterActionDefinition.TargetSpecies)
        {
            Monster target = FindSpeciesAlly(action.species);
            if (target != null && target != this && TrainingBattleManager.Instance != null)
            {
                return TrainingBattleManager.Instance.ResolveMonsterBarrierGain(target, Mathf.Max(0, action.amount));
            }
        }

        return PreviewBarrierGain(action.amount);
    }

    private int PreviewSelfBuffGain(MonsterActionDefinition action)
    {
        int gain = PreviewMonsterBuffAmount(action.buffId, action.amount);
        if (action.max > 0)
        {
            gain = Mathf.Clamp(action.max - GetBuffStack(action.buffId), 0, gain);
        }

        return gain;
    }

    // 패턴 계획 시점의 타격 횟수를 실행까지 고정
    private int ResolvePlannedHits(MonsterActionDefinition action)
    {
        if (!plannedHits.TryGetValue(action, out int hits))
        {
            hits = ResolveHits(action);
            if (action.hitsActivationBuff > 0)
            {
                // 패턴 계획은 턴 종료 후 진행되므로 그 시점까지의 발동 횟수를 고정
                plannedHits[action] = hits;
            }
        }

        return hits;
    }

    private int ResolveHits(MonsterActionDefinition action)
    {
        if (action.hitsBuff > 0)
        {
            return Mathf.Max(0, GetBuffStack(action.hitsBuff));
        }

        int hits = Mathf.Max(1, action.hits);
        if (action.hitsActivationBuff > 0)
        {
            hits += TrainingBattleManager.Instance?.GetMonsterBuffActivationCount(this, action.hitsActivationBuff) ?? 0;
        }

        if (action.hitsGrow)
        {
            hits += GetPatternUseCount(currentKey);
        }

        return hits;
    }

    private int GetPatternUseCount(string key)
    {
        return !string.IsNullOrEmpty(key) && patternUseCounts.TryGetValue(key, out int count) ? count : 0;
    }

    // ifSelfStatus는 현재 상태로 갈래를 골라 펼침
    private List<MonsterActionDefinition> FlattenActions(List<MonsterActionDefinition> actions)
    {
        List<MonsterActionDefinition> result = new List<MonsterActionDefinition>();
        if (actions == null)
        {
            return result;
        }

        foreach (MonsterActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (action.type == MonsterActionDefinition.IfSelfStatus)
            {
                result.AddRange(FlattenActions(GetBuffStack(action.buffId) > 0 ? action.then : action.@else));
                continue;
            }

            result.Add(action);
        }

        return result;
    }

    // ── 패턴 실행 ─────────────────────────────────────────────────────
    protected override void ExecuteAction(PlayerData target)
    {
        string executedKey = currentKey;
        MonsterPatternDefinition pattern = definition?.FindPattern(executedKey);
        lastUnblockedDamage = 0;
        hasAttackedInCurrentPattern = false;
        if (pattern != null)
        {
            foreach (MonsterActionDefinition action in FlattenActions(pattern.actions))
            {
                // 자폭 등 공격으로 파티가 먼저 쓰러지면 남은 행동(이탈 등)을 진행하지 않아 패배가 먼저 처리됨
                if (IsDead() || target == null || target.IsDead())
                {
                    break;
                }

                ExecuteMonsterAction(action, target);
            }
        }

        if (!string.IsNullOrEmpty(executedKey))
        {
            patternUseCounts[executedKey] = GetPatternUseCount(executedKey) + 1;
        }

        intentDamageScale = 1f;
        needsDecision = true;
        currentChangedByCard = false;
        plannedHits.Clear();
        OnPatternExecuted(executedKey);
    }

    private void ExecuteMonsterAction(MonsterActionDefinition action, PlayerData target)
    {
        switch (action.type)
        {
            case MonsterActionDefinition.Attack:
                hasAttackedInCurrentPattern = true;
                int hits = ResolvePlannedHits(action);
                int amount = Mathf.FloorToInt(Mathf.Max(0, action.amount) * intentDamageScale);
                for (int hitIndex = 0; hitIndex < hits; hitIndex++)
                {
                    lastUnblockedDamage += DealDamage(target, amount);
                    if (target.IsDead() || IsDead())
                    {
                        break;
                    }
                }
                break;
            case MonsterActionDefinition.Block:
                ExecuteBlock(action);
                break;
            case MonsterActionDefinition.Buff:
                ExecuteBuff(action);
                break;
            case MonsterActionDefinition.Debuff:
                AddDebuffToPlayer(target, action.buffId, action.amount);
                break;
            case MonsterActionDefinition.ReducePlayerBuff:
                if (BuffData.AllowsNegativeStack(action.buffId))
                {
                    target.AddBuff(action.buffId, -Mathf.Abs(action.amount));
                }
                else
                {
                    target.ConsumeBuffStack(action.buffId, Mathf.Abs(action.amount));
                }
                break;
            case MonsterActionDefinition.Heal:
                Heal(action.amount);
                break;
            case MonsterActionDefinition.HealUnblocked:
                Heal(lastUnblockedDamage);
                break;
            case MonsterActionDefinition.AddCard:
                ExecuteAddCards(action);
                break;
            case MonsterActionDefinition.Summon:
                SummonSpecies(action.species);
                break;
            case MonsterActionDefinition.Leave:
                // 공격 후 이탈은 자폭(의도 아이콘과 같은 기준) → 처치가 아닌 사망
                if (hasAttackedInCurrentPattern)
                {
                    SelfDestruct();
                }
                else
                {
                    LeaveCombat();
                }
                break;
            case MonsterActionDefinition.Special:
                ExecuteSpecial(action.key, action, target);
                break;
        }
    }

    private void ExecuteBlock(MonsterActionDefinition action)
    {
        switch (action.target)
        {
            case MonsterActionDefinition.TargetAllies:
                foreach (Monster monster in GetLivingMonsters())
                {
                    monster.AddDefense(action.amount);
                }
                break;
            case MonsterActionDefinition.TargetSpecies:
                (FindSpeciesAlly(action.species) ?? this).AddDefense(action.amount);
                break;
            default:
                AddDefense(action.amount);
                break;
        }
    }

    private void ExecuteBuff(MonsterActionDefinition action)
    {
        if (action.target == MonsterActionDefinition.TargetAllies)
        {
            foreach (Monster monster in GetLivingMonsters())
            {
                monster.AddBuff(action.buffId, action.amount);
            }
            return;
        }

        int gain = action.amount;
        if (action.max > 0)
        {
            gain = Mathf.Clamp(action.max - GetBuffStack(action.buffId), 0, gain);
        }

        if (gain > 0)
        {
            AddBuff(action.buffId, gain);
        }
    }

    private void ExecuteAddCards(MonsterActionDefinition action)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        int count = Mathf.Max(1, action.count);
        List<Card> handCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card card = CardManager.GetCardAsCard(action.cardId);
            if (card == null)
            {
                continue;
            }

            switch (action.zone)
            {
                case "hand":
                    handCards.Add(card);
                    break;
                case "drawShuffle":
                    battleManager.usableDeckManager.AddToDrawPileRandom(card);
                    break;
                default:
                    AddCardToPlayerDiscard(card);
                    break;
            }
        }

        // 몬스터가 만든 카드는 복제 생성(1008) 대상이 아님 (E-31)
        if (handCards.Count > 0 && battleManager.handManager != null && battleManager.CanGainCardsToHand())
        {
            battleManager.handManager.AddCard(battleManager.ProcessGeneratedCards(handCards, false));
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private void SummonSpecies(int species) // 추후 수정 필요
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        MonsterSpawner monsterSpawner = battleManager != null ? battleManager.monsterSpawner : null;
        if (monsterSpawner == null)
        {
            return;
        }

        Monster summonedMonster = monsterSpawner.SpawnSummonedMonster(definition.GetSpeciesIdInSameTier(species));
        if (summonedMonster == null)
        {
            // 실행 시점에 자리가 없으면 행동 없이 패턴을 넘김 (추후 "소환할 자리가 없군." 대사 출력 예정)
            return;
        }

        battleManager.RegisterMonster(summonedMonster);
        summonedMonster.PlanNextAction();
        summonedMonster.UpdateUI();
        battleManager.UpdateAllUI();
    }

    protected virtual void ExecuteSpecial(string key, MonsterActionDefinition action, PlayerData target)
    {
        Debug.LogWarning($"[DataMonster] {name}이(가) 처리하지 않는 특수 패턴입니다: {key}");
    }

    protected virtual void OnPatternExecuted(string patternKey)
    {
    }

    // 분기형 AI의 갈래 지정 (선지자 훔치기 결과)
    protected void SetAIBranch(string branchName)
    {
        ai?.SetBranch(branchName);
    }

    // ── 공용 조회 ─────────────────────────────────────────────────────
    protected static List<Monster> GetLivingMonsters()
    {
        List<Monster> livingMonsters = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.GetLivingMonsters()
            : null;
        return livingMonsters ?? new List<Monster>();
    }

    private Monster FindSpeciesAlly(int species)
    {
        foreach (Monster monster in GetLivingMonsters())
        {
            if (monster is DataMonster dataMonster && dataMonster.Species == species && !monster.IsDead())
            {
                return monster;
            }
        }

        return null;
    }

    private bool IsSummonPattern(string key)
    {
        MonsterPatternDefinition pattern = definition?.FindPattern(key);
        return pattern != null && ContainsAction(pattern.actions, MonsterActionDefinition.Summon);
    }

    private static bool TryReserveSummon()
    {
        MonsterSpawner monsterSpawner = TrainingBattleManager.Instance != null ? TrainingBattleManager.Instance.monsterSpawner : null;
        return monsterSpawner != null && monsterSpawner.TryReserveSummonSlot();
    }

    private bool IsAttackPattern(string key)
    {
        MonsterPatternDefinition pattern = definition?.FindPattern(key);
        return pattern != null && ContainsAction(pattern.actions, MonsterActionDefinition.Attack);
    }

    private static bool ContainsAction(List<MonsterActionDefinition> actions, string type)
    {
        if (actions == null)
        {
            return false;
        }

        foreach (MonsterActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (action.type == type
                || (action.type == MonsterActionDefinition.IfSelfStatus && (ContainsAction(action.then, type) || ContainsAction(action.@else, type))))
            {
                return true;
            }
        }

        return false;
    }

    // [buff:{buffId}] 틀에 넣을 인자 (버프 이름은 표시할 때 현재 언어로)
    private static Dictionary<string, object> BuffArguments(int buffId, int amount)
    {
        return new Dictionary<string, object> { { "buffId", buffId }, { "amount", amount } };
    }

    // ── IMonsterAIContext ─────────────────────────────────────────────
    bool IMonsterAIContext.IsAlone
    {
        get
        {
            foreach (Monster monster in GetLivingMonsters())
            {
                if (monster != this && !monster.IsDead())
                {
                    return false;
                }
            }

            return true;
        }
    }

    bool IMonsterAIContext.HasBlock => defense > 0;

    int IMonsterAIContext.GetSelfStatus(int buffId) => GetBuffStack(buffId);

    bool IMonsterAIContext.PlayerHasStatus(int buffId)
    {
        PlayerData player = TrainingBattleManager.Instance?.playerData;
        return player != null && player.GetBuffStack(buffId) > 0;
    }

    int IMonsterAIContext.SelfDebuffKinds => BuffCombatUtility.CountDistinctNegativeBuffTypes(this);

    bool IMonsterAIContext.IsGroupPatternTaken(IReadOnlyList<string> group)
    {
        foreach (Monster monster in GetLivingMonsters())
        {
            if (monster == this || !(monster is DataMonster other) || other.MonsterId != MonsterId)
            {
                continue;
            }

            string otherKey = other.CurrentPatternKey;
            if (!string.IsNullOrEmpty(otherKey))
            {
                foreach (string key in group)
                {
                    if (key == otherKey)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    int IMonsterAIContext.RandomRange(int maxExclusive) => Random.Range(0, Mathf.Max(1, maxExclusive));
}
