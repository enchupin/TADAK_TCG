using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class ThiefMonsterBuffScript : MonsterBuffScript
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
