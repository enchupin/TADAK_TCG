using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
// 3075 저주 회복: 몬스터 턴 종료 시 힘 {stack}을 얻고 제거 (시간 저주로 잃은 힘 복구)
public sealed class CurseRecoveryBuffScript : MonsterBuffScript
{
    public override int BuffId => CurseRecoveryBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.AddBuff(StrengthBuffId, stack);
        monster.RemoveBuffStack(BuffId);
    }
}
}
