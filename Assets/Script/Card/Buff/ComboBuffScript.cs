public sealed class ComboBuffScript : PlayerBuffScript
{
    private const int RequiredStack = 7;

    // 공격 타격으로 들어온 피해만 세기 위해 공격 시작 시 표시
    private bool isAttackHitPending;

    public override int BuffId => CreamBuffRuntimeUtility.ComboBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        isAttackHitPending = false;
    }

    public override float GetCalculatedCardBaseMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack < RequiredStack)
        {
            return currentMultiplier;
        }

        return currentMultiplier * 2f;
    }

    public override void OnPlayerAttackStarted(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player != null && stack >= RequiredStack)
        {
            player.RemoveBuffStack(BuffId);
            isAttackHitPending = false;
            return;
        }

        isAttackHitPending = true;
    }

    public override void OnPlayerDamageDealt(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int dealtDamage, int stack)
    {
        bool isAttackHit = isAttackHitPending;
        isAttackHitPending = false;
        if (player == null || stack <= 0 || dealtDamage <= 0 || !isAttackHit)
        {
            return;
        }

        player.AddBuff(BuffId, 1);
    }
}
