using System.Collections.Generic;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class ThiefBuffScript : MonsterBuffScript
{
    private static readonly Dictionary<Monster, List<Card>> StolenCardsByMonster = new();

    public override int BuffId => ThiefBuffId;

    public static void RegisterStolenCard(Monster monster, Card stolenCard)
    {
        if (monster == null || stolenCard == null)
        {
            return;
        }

        if (!StolenCardsByMonster.TryGetValue(monster, out List<Card> stolenCards))
        {
            stolenCards = new List<Card>();
            StolenCardsByMonster[monster] = stolenCards;
        }

        stolenCards.Add(stolenCard);
    }

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        ReturnStolenCards(battleManager, monster, stack);
    }

    public override void OnMonsterLeaveCombat(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null)
        {
            // R-47a 사망하지 않고 이탈하면 훔쳐 간 카드는 저장 덱에서 영구 제거
            if (StolenCardsByMonster.TryGetValue(monster, out List<Card> stolenCards))
            {
                battleManager?.QueueStolenCardsForPermanentRemoval(stolenCards);
            }

            StolenCardsByMonster.Remove(monster);
        }
    }

    private static void ReturnStolenCards(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (battleManager?.usableDeckManager == null || monster == null)
        {
            return;
        }

        if (!StolenCardsByMonster.TryGetValue(monster, out List<Card> stolenCards) || stolenCards.Count <= 0)
        {
            return;
        }

        int returnedCount = 0;
        foreach (Card stolenCard in stolenCards)
        {
            if (stolenCard == null)
            {
                continue;
            }

            battleManager.usableDeckManager.AddToDrawPileRandom(stolenCard);
            returnedCount++;
        }

        StolenCardsByMonster.Remove(monster);
        if (returnedCount > 0)
        {
            monster.ConsumeBuffStack(ThiefBuffId, returnedCount);
            battleManager.UpdateAllUI();
        }
    }
}
}
