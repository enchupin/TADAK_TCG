using System.Collections.Generic;
using static BattleRuntimeDefinitions;

// 3061 촉매: 다음 포션이 대상(없으면 모든 적)이 보유한 반응 상태를 각각 {stack}만큼 추가 부여하고 모두 소모 (R-78)
public sealed class CatalystBuffScript : PlayerBuffScript
{
    public override int BuffId => CatalystBuffId;

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || isRepeatedEffect || stack <= 0 || !BuffCardUtility.IsPotionCard(playedCard))
        {
            return;
        }

        player.RemoveBuffStack(BuffId);
        List<Monster> targets = originalTarget != null && !originalTarget.IsDead()
            ? new List<Monster> { originalTarget }
            : battleManager.GetLivingMonsters();
        foreach (Monster monster in targets)
        {
            foreach (int reactiveBuffId in GameRules.Groups.reactive)
            {
                if (monster == null || monster.IsDead() || monster.GetBuffStack(reactiveBuffId) <= 0)
                {
                    continue;
                }

                battleManager.ApplyBuffToMonster(monster, reactiveBuffId, stack);
            }
        }

        battleManager.UpdateAllUI();
    }
}
