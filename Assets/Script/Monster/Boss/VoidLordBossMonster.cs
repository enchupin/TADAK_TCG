using System.Collections.Generic;

// v1.0 304 공허 군주
// 체력·패턴·AI는 monsters.json 데이터 사용 (DataMonster)
// 해로운 효과 종류가 3개 이상이면 턴 종료 시 고정 순환 [P04, P05, P03]으로 전환 (lock)
// P04 해로운 효과를 모두 제거하고 다음 행동(P05)까지 피해를 받지 않음
public class VoidLordBossMonster : DataMonster
{
    private const string CleansePatternKey = "P04";

    private bool isInvulnerable;

    protected override void OnBattleStart()
    {
        isInvulnerable = false;
        base.OnBattleStart();
    }

    protected override bool CanReceiveDamage(int incomingDamage)
    {
        return !isInvulnerable;
    }

    protected override IEnumerable<MonsterIntentIconType> GetSpecialIcons(string key)
    {
        yield return MonsterIntentIconType.BeneficialEffect;
    }

    protected override void ExecuteSpecial(string key, MonsterActionDefinition action, PlayerData target)
    {
        if (key == "cleanse_invulnerable")
        {
            BuffCombatUtility.RemoveAllNegativeBuffs(this);
            isInvulnerable = true;
            UpdateUI();
            return;
        }

        base.ExecuteSpecial(key, action, target);
    }

    protected override void OnPatternExecuted(string patternKey)
    {
        // 무적은 P04 다음 행동을 마칠 때 해제
        if (patternKey != CleansePatternKey)
        {
            isInvulnerable = false;
        }
    }
}
