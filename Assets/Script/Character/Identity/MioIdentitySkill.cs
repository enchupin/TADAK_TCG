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

        battleManager.ApplyBuffToAllEnemies(DrowningBuffId, 15);

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        foreach (Monster monster in livingMonsters)
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            if (monster.GetBuffStack(DrowningBuffId) > monster.maxHP)
            {
                monster.Kill();
            }
        }

        return true;
    }
}
