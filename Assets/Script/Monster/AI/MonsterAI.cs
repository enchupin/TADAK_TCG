using System;
using System.Collections.Generic;

// 몬스터 AI가 조건을 판정할 때 쓰는 전투 상태
public interface IMonsterAIContext
{
    // 살아 있는 다른 몬스터가 없음
    bool IsAlone { get; }
    bool HasBlock { get; }
    int GetSelfStatus(int buffId);
    bool PlayerHasStatus(int buffId);
    // 보유한 해로운 효과 종류 수
    int SelfDebuffKinds { get; }
    // 같은 종류의 다른 개체가 이 패턴 묶음 중 하나를 의도로 가지고 있음 (group_exclusive)
    bool IsGroupPatternTaken(IReadOnlyList<string> group);
    int RandomRange(int maxExclusive);
}

// 7절 AI 형식 실행기: cycle · sequence · random · rules · branch + override · require · group_exclusive · lock
// NextPattern은 다음 의도를 확정하며 상태를 진행함 (의도 큐가 채울 때도 같은 함수 사용)
public sealed class MonsterAI
{
    private readonly MonsterAIDefinition definition;
    private readonly List<string> patternKeys;

    private int cycleIndex;
    private int sequenceIndex;
    private bool startUsed;
    private string lastKey;
    private string branchName;
    private int branchIndex;
    private readonly Dictionary<int, int> ruleLoopIndices = new Dictionary<int, int>();
    private int lastRuleIndex = -1;
    private bool lockActive;
    private int lockIndex;

    public MonsterAI(MonsterAIDefinition definition, IEnumerable<string> patternKeys)
    {
        this.definition = definition ?? new MonsterAIDefinition();
        this.patternKeys = new List<string>(patternKeys ?? Array.Empty<string>());
    }

    public MonsterAIDefinition Definition => definition;
    public bool IsRules => definition.type == MonsterAIDefinition.Rules;
    public bool DecideAtTurnEnd => definition.decideAtTurnEnd;
    public bool LockDecidesAtTurnEnd => definition.@lock != null && definition.@lock.decideAtTurnEnd;
    public bool IsLockActive => lockActive;
    public string BranchName => branchName;
    public int PatternCount => patternKeys.Count;

    // R-86 같은 종류 k번째 개체는 순환형·순서형 순환을 k칸 뒤에서 시작 (시작 패턴 지정이 없을 때)
    public bool SupportsStagger => string.IsNullOrEmpty(definition.start)
        && (definition.type == MonsterAIDefinition.Cycle || definition.type == MonsterAIDefinition.Sequence);

    public void ApplyStagger(int offset)
    {
        if (!SupportsStagger || offset <= 0)
        {
            return;
        }

        if (definition.type == MonsterAIDefinition.Cycle)
        {
            cycleIndex += offset;
        }
        else
        {
            sequenceIndex += offset;
        }
    }

    public void Reset()
    {
        cycleIndex = 0;
        sequenceIndex = 0;
        startUsed = false;
        lastKey = null;
        branchName = null;
        branchIndex = 0;
        ruleLoopIndices.Clear();
        lastRuleIndex = -1;
        lockActive = false;
        lockIndex = 0;
    }

    // 조건 충족 시 우선 패턴 (큐보다 우선, 상태 진행 없음)
    public string CheckOverride(IMonsterAIContext context)
    {
        if (definition.@override == null)
        {
            return null;
        }

        foreach (MonsterAIRule rule in definition.@override)
        {
            if (rule != null && !string.IsNullOrEmpty(rule.use) && Evaluate(rule.@if, context))
            {
                return rule.use;
            }
        }

        return null;
    }

    // lock 조건이 충족되면 고정 순환을 시작
    public bool TryActivateLock(IMonsterAIContext context)
    {
        MonsterAILock lockDefinition = definition.@lock;
        if (lockDefinition == null || lockActive || lockDefinition.loop == null || lockDefinition.loop.Count == 0)
        {
            return false;
        }

        if (!Evaluate(lockDefinition.@if, context))
        {
            return false;
        }

        lockActive = true;
        lockIndex = 0;
        return true;
    }

    // 분기형: 시작 패턴 결과로 갈래 선택
    public void SetBranch(string name)
    {
        branchName = name;
        branchIndex = 0;
    }

    public string NextPattern(IMonsterAIContext context)
    {
        string key = ResolveNext(context);
        lastKey = key;
        return key;
    }

    // 조건 규칙형: 조건 변화로 다른 규칙이 맞게 되면 방금 결정을 되돌리고 다시 결정 (E-40)
    public bool TryReevaluateRules(IMonsterAIContext context, string currentKey, out string newKey)
    {
        newKey = currentKey;
        if (!IsRules || lockActive || definition.rules == null || lastRuleIndex < 0)
        {
            return false;
        }

        int matchedIndex = FindMatchingRule(context);
        if (matchedIndex < 0 || matchedIndex == lastRuleIndex)
        {
            return false;
        }

        MonsterAIRule previousRule = definition.rules[lastRuleIndex];
        if (previousRule.loop != null && previousRule.loop.Count > 0 && ruleLoopIndices.TryGetValue(lastRuleIndex, out int previousIndex))
        {
            ruleLoopIndices[lastRuleIndex] = Math.Max(0, previousIndex - 1);
        }

        newKey = UseRule(matchedIndex);
        lastKey = newKey;
        return true;
    }

    private string ResolveNext(IMonsterAIContext context)
    {
        if (lockActive)
        {
            List<string> lockLoop = definition.@lock.loop;
            string lockKey = lockLoop[lockIndex];
            lockIndex++;
            if (lockIndex >= lockLoop.Count)
            {
                lockActive = false;
                lockIndex = 0;
            }

            return lockKey;
        }

        if (!startUsed && !string.IsNullOrEmpty(definition.start))
        {
            startUsed = true;
            return definition.start;
        }

        startUsed = true;
        switch (definition.type)
        {
            case MonsterAIDefinition.Sequence:
                return NextSequence();
            case MonsterAIDefinition.Random:
                return NextRandom(context);
            case MonsterAIDefinition.Rules:
                return NextRule(context);
            case MonsterAIDefinition.Branch:
                return NextBranch();
            default:
                return NextCycle();
        }
    }

    private string NextCycle()
    {
        List<string> order = definition.order != null && definition.order.Count > 0 ? definition.order : patternKeys;
        if (order.Count == 0)
        {
            return null;
        }

        string key = order[cycleIndex % order.Count];
        cycleIndex++;
        return key;
    }

    private string NextSequence()
    {
        List<string> intro = definition.intro ?? new List<string>();
        List<string> loop = definition.loop ?? new List<string>();
        string key;
        if (sequenceIndex < intro.Count)
        {
            key = intro[sequenceIndex];
        }
        else if (loop.Count > 0)
        {
            key = loop[(sequenceIndex - intro.Count) % loop.Count];
        }
        else
        {
            key = intro.Count > 0 ? intro[intro.Count - 1] : null;
        }

        sequenceIndex++;
        return key;
    }

    private string NextRandom(IMonsterAIContext context)
    {
        List<string> candidates = new List<string>();
        foreach (string key in patternKeys)
        {
            if (IsRandomCandidate(key, context))
            {
                candidates.Add(key);
            }
        }

        // 후보가 없으면 연속 금지만 적용
        if (candidates.Count == 0)
        {
            candidates.AddRange(patternKeys.FindAll(key => !IsBlockedByNoRepeat(key)));
        }

        if (candidates.Count == 0)
        {
            candidates.AddRange(patternKeys);
        }

        return candidates.Count > 0 ? candidates[context.RandomRange(candidates.Count)] : null;
    }

    private bool IsRandomCandidate(string key, IMonsterAIContext context)
    {
        if (IsBlockedByNoRepeat(key))
        {
            return false;
        }

        if (definition.require != null)
        {
            foreach (MonsterPatternRequirement requirement in definition.require)
            {
                if (requirement != null && requirement.pattern == key && !Evaluate(requirement.@if, context))
                {
                    return false;
                }
            }
        }

        if (definition.groupExclusive != null && definition.groupExclusive.Contains(key)
            && context.IsGroupPatternTaken(definition.groupExclusive))
        {
            return false;
        }

        return true;
    }

    private bool IsBlockedByNoRepeat(string key)
    {
        if (lastKey == null || key != lastKey)
        {
            return false;
        }

        return definition.noRepeatAll || (definition.noRepeat != null && definition.noRepeat.Contains(key));
    }

    private string NextRule(IMonsterAIContext context)
    {
        int ruleIndex = FindMatchingRule(context);
        if (ruleIndex < 0)
        {
            return NextCycle();
        }

        return UseRule(ruleIndex);
    }

    private int FindMatchingRule(IMonsterAIContext context)
    {
        if (definition.rules == null)
        {
            return -1;
        }

        for (int i = 0; i < definition.rules.Count; i++)
        {
            MonsterAIRule rule = definition.rules[i];
            if (rule != null && Evaluate(rule.@if, context))
            {
                return i;
            }
        }

        return -1;
    }

    private string UseRule(int ruleIndex)
    {
        lastRuleIndex = ruleIndex;
        MonsterAIRule rule = definition.rules[ruleIndex];
        if (rule.loop != null && rule.loop.Count > 0)
        {
            ruleLoopIndices.TryGetValue(ruleIndex, out int loopIndex);
            ruleLoopIndices[ruleIndex] = loopIndex + 1;
            return rule.loop[loopIndex % rule.loop.Count];
        }

        return rule.use;
    }

    private string NextBranch()
    {
        MonsterAIBranch branch = definition.branches?.Find(candidate => candidate != null && candidate.name == branchName)
            ?? (definition.branches != null && definition.branches.Count > 0 ? definition.branches[definition.branches.Count - 1] : null);
        if (branch == null || branch.loop == null || branch.loop.Count == 0)
        {
            return definition.start;
        }

        string key = branch.loop[branchIndex % branch.loop.Count];
        branchIndex++;
        return key;
    }

    public static bool Evaluate(MonsterAICondition condition, IMonsterAIContext context)
    {
        if (condition == null || string.IsNullOrEmpty(condition.type))
        {
            return true;
        }

        switch (condition.type)
        {
            case MonsterAICondition.Alone:
                return context.IsAlone;
            case MonsterAICondition.HasBlock:
                return context.HasBlock;
            case MonsterAICondition.SelfStatusLe:
                return context.GetSelfStatus(condition.buffId) <= condition.value;
            case MonsterAICondition.SelfStatusGe:
                return context.GetSelfStatus(condition.buffId) >= condition.value;
            case MonsterAICondition.PlayerLacks:
                return !context.PlayerHasStatus(condition.buffId);
            case MonsterAICondition.DebuffKindsGe:
                return context.SelfDebuffKinds >= condition.value;
            default:
                return false;
        }
    }
}
