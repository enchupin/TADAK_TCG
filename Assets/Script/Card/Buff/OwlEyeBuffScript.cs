using static BattleRuntimeDefinitions;

// 3074 부엉이의 눈: 적에게 해로운 효과를 부여할 때마다 그 적에게 깃털 {stack}
public sealed class OwlEyeBuffScript : PlayerBuffScript
{
    private bool isApplyingFeather;

    public override int BuffId => OwlEyeBuffId;

    public override void OnEnemyDebuffApplied(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int crueltyStackBeforeApply)
    {
        // 깃털 부여 자체에는 반응하지 않아 연쇄 부여를 막음
        if (battleManager == null || targetMonster == null || targetMonster.IsDead() || stack <= 0
            || buffId == FeatherBuffId || isApplyingFeather)
        {
            return;
        }

        isApplyingFeather = true;
        try
        {
            battleManager.ApplyBuffToMonster(targetMonster, FeatherBuffId, stack);
        }
        finally
        {
            isApplyingFeather = false;
        }
    }
}
