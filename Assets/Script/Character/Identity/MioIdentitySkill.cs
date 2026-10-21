using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class MioIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        // v1.0: 모든 적 익사 10 (최대 체력 초과 시 즉시 발동), 물결 2
        battleManager.ApplyBuffToAllEnemies(DrowningBuffId, 10);

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        foreach (Monster monster in livingMonsters)
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            if (monster.GetBuffStack(DrowningBuffId) > monster.maxHP)
            {
                BuffCombatUtility.KillByDrowning(battleManager, monster);
            }
        }

        battleManager.ApplyBuffToPlayer(WaveBuffId, 2);

        return true;
    }
}
