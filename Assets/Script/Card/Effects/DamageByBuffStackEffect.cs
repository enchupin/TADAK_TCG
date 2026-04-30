using UnityEngine;

[System.Serializable]
public sealed class DamageByBuffStackEffect : ICardEffect
{
    public int buffId;
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null || buffId <= 0)
        {
            return;
        }

        int totalDamageDealt = 0;
        foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            int stack = Mathf.Max(0, monster.GetBuffStack(buffId));
            int damage = battleManager.ResolvePlayerEffectDamage(stack);
            if (damage <= 0)
            {
                continue;
            }

            int dealtDamage = monster.TakeDamage(damage, 0);
            totalDamageDealt += dealtDamage;
            battleManager.HandlePlayerDamageDealt(monster, dealtDamage);
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);
        battleManager.UpdateAllUI();
    }
}
