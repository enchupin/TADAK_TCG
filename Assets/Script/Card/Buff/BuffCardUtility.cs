using System.Collections.Generic;
using UnityEngine;

public static class BuffCardUtility
{
    public const string BasePotionGroupName = "isla_potion_even_base_pool";
    public const string JokerUpgradeGroupName = "enforce_102040";

    public static bool IsPotionCard(Card card)
    {
        return card != null && IsPotionCardId(card.cardId);
    }

    public static bool IsPotionCardId(int cardId)
    {
        return cardId >= 101080 && cardId <= 101087;
    }

    public static List<Card> CreateRandomCardsFromGroup(string groupName, int count)
    {
        List<Card> generatedCards = new();
        if (count <= 0)
        {
            return generatedCards;
        }

        List<int> pool = CardManager.GetCardIdsByGroup(groupName);
        if (pool == null || pool.Count == 0)
        {
            return generatedCards;
        }

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            Card generatedCard = CardManager.GetCardAsCard(pool[index]);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        return generatedCards;
    }

    public static List<Card> CreateRandomUniqueUpgradeCards(int count)
    {
        List<int> pool = GetUniqueUpgradeCardPool();
        if (pool.Count == 0)
        {
            return CreateRandomCardsFromGroup(JokerUpgradeGroupName, count);
        }

        List<Card> generatedCards = new();
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            Card generatedCard = CardManager.GetCardAsCard(pool[index]);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        return generatedCards;
    }

    public static Monster PickRandomLivingMonster(TrainingBattleManager battleManager)
    {
        List<Monster> livingMonsters = battleManager?.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, livingMonsters.Count);
        return livingMonsters[index];
    }

    public static bool HasAttackEffect(Card card)
    {
        return HasAttackEffect(card?.effects);
    }

    public static bool HasAttackEffect(List<ICardEffect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return false;
        }

        foreach (ICardEffect effect in effects)
        {
            if (effect is AttackEffect)
            {
                return true;
            }

            if (effect is RepeatEffect repeatEffect && repeatEffect.effectToRepeat != null)
            {
                if (HasAttackEffect(new List<ICardEffect> { repeatEffect.effectToRepeat }))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<int> GetUniqueUpgradeCardPool()
    {
        List<int> pool = new();
        HashSet<Character> selectedCharacters = null;
        if (SelectedButtonControl.selectedCharacterList != null && SelectedButtonControl.selectedCharacterList.Count > 0)
        {
            selectedCharacters = new HashSet<Character>(SelectedButtonControl.selectedCharacterList);
        }

        List<CardData> allCards = CardManager.GetAllCards();
        foreach (CardData cardData in allCards)
        {
            if (!IsUniqueUpgradeCard(cardData, selectedCharacters))
            {
                continue;
            }

            if (!pool.Contains(cardData.cardId))
            {
                pool.Add(cardData.cardId);
            }
        }

        return pool;
    }

    private static bool IsUniqueUpgradeCard(CardData cardData, HashSet<Character> selectedCharacters)
    {
        if (cardData == null)
        {
            return false;
        }

        if (selectedCharacters != null && !selectedCharacters.Contains(cardData.character))
        {
            return false;
        }

        if (Mathf.Abs(cardData.cardId) % 10 == 0)
        {
            return false;
        }

        return cardData.keywords != null && cardData.keywords.Contains(CardKeywordIds.Unique);
    }
}
