using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class DrowningBuffScript : MonsterBuffScript
{
    public override int BuffId => DrowningBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0 || monster.IsDead() || monster.hp >= stack)
        {
            return;
        }

        monster.Kill();
        battleManager?.UpdateAllUI();
    }
}
}
