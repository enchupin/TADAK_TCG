using System;
using System.Collections.Generic;

// v1.0 몬스터 데이터 (Resources/JsonData/monsters.json, 생성: .claude-work/monstergen/gen.py)
// 계층마다 독립된 체력·패턴 수치를 가지며 배율 단계가 없음 (R-88)
[Serializable]
public sealed class MonsterDefinitionRoot
{
    public string version;
    public List<MonsterDefinition> monsters = new List<MonsterDefinition>();
}

[Serializable]
public sealed class MonsterDefinition
{
    public const string KindNormal = "normal";
    public const string KindNamed = "named";
    public const string KindBoss = "boss";

    // 4자리 = 계층 1 + 계열 1 + 번호 2
    public int id;
    // 3자리 종 (계열 + 번호), 소환·아군 지정은 같은 계층의 종
    public int species;
    // 몬스터 컴포넌트 클래스 이름 (이미지 이름 기준)
    public string className;
    public string name;
    public int tier;
    public string kind;
    public int hp;
    public List<MonsterStartEffect> start = new List<MonsterStartEffect>();
    public int startBlock;
    public List<MonsterPatternDefinition> patterns = new List<MonsterPatternDefinition>();
    public MonsterAIDefinition ai = new MonsterAIDefinition();

    public bool IsBoss => string.Equals(kind, KindBoss, StringComparison.OrdinalIgnoreCase);

    public MonsterPatternDefinition FindPattern(string key)
    {
        if (patterns == null || string.IsNullOrEmpty(key))
        {
            return null;
        }

        return patterns.Find(pattern => pattern != null && pattern.key == key);
    }

    public int GetSpeciesIdInSameTier(int targetSpecies)
    {
        return tier * 1000 + targetSpecies;
    }
}

[Serializable]
public sealed class MonsterStartEffect
{
    public int buffId;
    public int amount;
}

[Serializable]
public sealed class MonsterPatternDefinition
{
    // 7절 표기 P01 (패턴 ID 끝 두 자리)
    public string key;
    // 6자리 = 몬스터 ID × 100 + 번호
    public int id;
    // 명세 문구 (특수 패턴의 의도 표시에 사용)
    public string text;
    public List<MonsterActionDefinition> actions = new List<MonsterActionDefinition>();
}

[Serializable]
public sealed class MonsterActionDefinition
{
    public const string Attack = "attack";
    public const string Block = "block";
    public const string Buff = "buff";
    public const string Debuff = "debuff";
    public const string ReducePlayerBuff = "reducePlayerBuff";
    public const string Heal = "heal";
    public const string HealUnblocked = "healUnblocked";
    public const string AddCard = "card";
    public const string Summon = "summon";
    public const string Leave = "leave";
    public const string Nothing = "nothing";
    public const string Special = "special";
    public const string IfSelfStatus = "ifSelfStatus";

    public const string TargetSelf = "self";
    public const string TargetAllies = "allies";
    public const string TargetSpecies = "species";

    public string type;
    public int amount;
    public int hits = 1;
    // 타격 횟수 = 해당 버프 중첩 (여덟 다리)
    public int hitsBuff;
    // 타격 횟수 = hits + 해당 버프 발동 횟수 (미래 포식)
    public int hitsActivationBuff;
    // 타격 횟수 = hits + 이 패턴을 사용한 횟수 (공허 괴수)
    public bool hitsGrow;
    public int buffId;
    public string target;
    public int species;
    public int cardId;
    public int count = 1;
    // hand · discard · drawShuffle
    public string zone;
    // 버프 획득 상한 (선인장 가시 최대 9)
    public int max;
    // special 키 (종 클래스가 처리)
    public string key;
    public List<MonsterActionDefinition> then = new List<MonsterActionDefinition>();
    public List<MonsterActionDefinition> @else = new List<MonsterActionDefinition>();
}

[Serializable]
public sealed class MonsterAIDefinition
{
    public const string Cycle = "cycle";
    public const string Sequence = "sequence";
    public const string Random = "random";
    public const string Rules = "rules";
    public const string Branch = "branch";

    public string type = Cycle;
    public List<string> order = new List<string>();
    public List<string> intro = new List<string>();
    public List<string> loop = new List<string>();
    public string start;
    public List<string> noRepeat = new List<string>();
    public bool noRepeatAll;
    public List<MonsterPatternRequirement> require = new List<MonsterPatternRequirement>();
    public List<string> groupExclusive = new List<string>();
    public List<MonsterAIRule> @override = new List<MonsterAIRule>();
    public List<MonsterAIRule> rules = new List<MonsterAIRule>();
    public List<MonsterAIBranch> branches = new List<MonsterAIBranch>();
    // 의도를 몬스터 턴 종료 시 결정
    public bool decideAtTurnEnd;
    public MonsterAILock @lock;
}

[Serializable]
public sealed class MonsterAICondition
{
    public const string Alone = "alone";
    public const string HasBlock = "has_block";
    public const string SelfStatusLe = "self_status_le";
    public const string SelfStatusGe = "self_status_ge";
    public const string PlayerLacks = "player_lacks";
    public const string DebuffKindsGe = "debuff_kinds_ge";

    public string type;
    public int buffId;
    public int value;
}

[Serializable]
public sealed class MonsterPatternRequirement
{
    public string pattern;
    public MonsterAICondition @if;
}

[Serializable]
public sealed class MonsterAIRule
{
    public MonsterAICondition @if;
    public string use;
    public List<string> loop;
}

[Serializable]
public sealed class MonsterAIBranch
{
    public string name;
    public List<string> loop = new List<string>();
}

[Serializable]
public sealed class MonsterAILock
{
    public MonsterAICondition @if;
    public List<string> loop = new List<string>();
    public bool decideAtTurnEnd;
}

// 조우 데이터 (Resources/JsonData/encounters.json)
[Serializable]
public sealed class EncounterDefinitionRoot
{
    public string version;
    public List<EncounterDefinition> encounters = new List<EncounterDefinition>();
}

[Serializable]
public sealed class EncounterDefinition
{
    // E101 · NM02-1 · BS01
    public string id;
    public string kind;
    // 1 ~ 4 계층, 5 보스
    public int tier;
    public List<int> monsters = new List<int>();
}
