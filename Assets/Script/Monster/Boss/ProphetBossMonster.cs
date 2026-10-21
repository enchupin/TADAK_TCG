using System.Collections.Generic;
using UnityEngine;

// v1.0 302 선지자
// 체력·패턴·AI는 monsters.json 데이터 사용 (DataMonster)
// P01 이로운 효과 훔치기 결과로 갈래 선택: 성공 stole [P02, P03, P01] · 실패 failed [P04, P05, P01]
public class ProphetBossMonster : DataMonster
{
    private const string BranchStole = "stole";
    private const string BranchFailed = "failed";

    protected override IEnumerable<MonsterIntentIconType> GetSpecialIcons(string key)
    {
        yield return MonsterIntentIconType.BeneficialEffect;
        yield return MonsterIntentIconType.HarmfulEffect;
    }

    protected override void ExecuteSpecial(string key, MonsterActionDefinition action, PlayerData target)
    {
        if (key == "steal_buff")
        {
            SetAIBranch(TryStealPlayerBeneficialBuff() ? BranchStole : BranchFailed);
            return;
        }

        base.ExecuteSpecial(key, action, target);
    }

    private bool TryStealPlayerBeneficialBuff()
    {
        PlayerData player = TrainingBattleManager.Instance != null ? TrainingBattleManager.Instance.playerData : null;
        if (player == null)
        {
            return false;
        }

        Buff stolenBuff = BuffCombatUtility.RemoveRandomBeneficialBuff(player);
        if (stolenBuff == null)
        {
            return false;
        }

        AddBuff(stolenBuff.data.buffId, Mathf.Max(1, stolenBuff.stack));
        TrainingBattleManager.Instance?.UpdateAllUI();
        return true;
    }
}
