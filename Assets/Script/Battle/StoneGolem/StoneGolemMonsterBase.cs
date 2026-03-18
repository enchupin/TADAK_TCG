using System.Collections.Generic;

public abstract class StoneGolemMonsterBase : Monster
{
    protected bool HasOtherLivingStoneGolem()
    {
        List<Monster> livingMonsters = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.GetLivingMonsters()
            : null;
        if (livingMonsters == null)
        {
            return false;
        }

        foreach (Monster monster in livingMonsters)
        {
            if (monster == null || monster == this || monster.IsDead())
            {
                continue;
            }

            if (monster.MonsterId == 106 || monster.MonsterId == 107 || monster.MonsterId == 108)
            {
                return true;
            }
        }

        return false;
    }
}
