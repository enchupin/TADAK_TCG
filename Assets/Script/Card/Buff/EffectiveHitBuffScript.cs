using static BattleRuntimeDefinitions;

// 3064 유효타: 막히지 않은 공격 피해를 입힐 때마다 득점 {stack}
public sealed class EffectiveHitBuffScript : PlayerBuffScript
{
    private bool isAttackHitPending;

    public override int BuffId => EffectiveHitBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        isAttackHitPending = false;
    }

    public override void OnPlayerAttackStarted(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        isAttackHitPending = true;
    }

    public override void OnPlayerDamageDealt(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int dealtDamage, int stack)
    {
        bool isAttackHit = isAttackHitPending;
        isAttackHitPending = false;
        if (battleManager == null || !isAttackHit || dealtDamage <= 0 || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(ScoreBuffId, stack);
    }
}
