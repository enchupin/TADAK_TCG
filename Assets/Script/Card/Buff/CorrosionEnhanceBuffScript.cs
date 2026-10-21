using static BattleRuntimeDefinitions;

public sealed class CorrosionEnhanceBuffScript : PlayerBuffScript
{
    // 효과는 몬스터 부식 배율 계산(MonsterBuffs.CorrosionBuffScript)에서 이 버프 보유 여부로 처리
    public override int BuffId => CorrosionEnhanceBuffId;
}
