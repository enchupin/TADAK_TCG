using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
// 2003 덫 상태: 보호막을 얻을 수 없고 몬스터의 다음 턴 시작 시 제거
public sealed class TrapStateBuffScript : MonsterBuffScript
{
    public override int BuffId => TrapStateBuffId;

    public override int ModifyBarrierGain(TrainingBattleManager battleManager, Monster monster, int stack, int currentGain)
    {
        return stack > 0 ? 0 : currentGain;
    }

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        monster?.RemoveBuffStack(BuffId);
    }
}
}
