using System.Collections.Generic;
using UnityEngine;

public class MushroomHostMonster : Monster
{
    private static readonly int[] OpeningPatternSequence = { 20301, 20302, 20303, 20304, 20301, 20302, 20303 };
    private int actionCount;

    public override int MonsterId => 203;
    protected override string MonsterName => "버섯 숙주";
    protected override int BaseMaxHp => 180;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.ParasiticMushroomBuffId, 1);
    }

    protected override bool IsNonStackableBuff(int buffId)
    {
        return buffId == BattleRuntimeDefinitions.ParasiticMushroomBuffId
            || buffId == BattleRuntimeDefinitions.PoisonUpgradeBuffId
            || base.IsNonStackableBuff(buffId);
    }

    protected override void BuildNextAction()
    {
        switch (GetCurrentPatternId())
        {
            case 20301:
                SetIntent($"적의 손에 {GetGeneratedPoisonCardName()} 카드를 2장 생성합니다.");
                SetPlannedPattern(20301, MonsterIntentIconType.DisruptCard);
                break;
            case 20302:
                SetAttackIntent(16, "피해를 16 입힙니다. 빈약을 2 부여합니다.");
                SetPlannedPattern(20302, MonsterIntentIconType.Attack, MonsterIntentIconType.HarmfulEffect);
                break;
            case 20303:
                SetAttackIntent(21, "피해를 21 입힙니다.");
                SetPlannedPattern(20303, MonsterIntentIconType.Attack);
                break;
            default:
                SetIntent("중독 강화를 얻습니다.");
                SetPlannedPattern(20304, MonsterIntentIconType.BeneficialEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (GetCurrentPatternId())
        {
            case 20301:
                AddPoisonCardsToPlayerHand(2);
                break;
            case 20302:
                if (DealDamage(target, 16) > 0)
                {
                    ReducePlayerMaxHp(target, 2);
                }

                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 2);
                break;
            case 20303:
                if (DealDamage(target, 21) > 0)
                {
                    ReducePlayerMaxHp(target, 2);
                }
                break;
            default:
                AddBuff(BattleRuntimeDefinitions.PoisonUpgradeBuffId, 1);
                UpgradeAllPoisonCards();
                break;
        }

        actionCount++;
    }

    private int GetCurrentPatternId()
    {
        if (actionCount < OpeningPatternSequence.Length)
        {
            return OpeningPatternSequence[actionCount];
        }

        int repeatIndex = (actionCount - OpeningPatternSequence.Length) % 3;
        return OpeningPatternSequence[repeatIndex];
    }

    private string GetGeneratedPoisonCardName()
    {
        return HasPoisonUpgrade() ? "중독+" : "중독";
    }

    private bool HasPoisonUpgrade()
    {
        return GetBuffStack(BattleRuntimeDefinitions.PoisonUpgradeBuffId) > 0;
    }

    private void AddPoisonCardsToPlayerHand(int count)
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager == null || battleManager.handManager == null || !battleManager.CanGainCardsToHand())
        {
            return;
        }

        int poisonCardId = HasPoisonUpgrade()
            ? BattleRuntimeDefinitions.PoisonPlusCardId
            : BattleRuntimeDefinitions.PoisonCardId;

        List<Card> generatedCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card generatedCard = CardManager.GetCardAsCard(poisonCardId);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        List<Card> processedCards = battleManager.ProcessGeneratedCards(generatedCards, true);
        if (processedCards.Count == 0)
        {
            return;
        }

        battleManager.handManager.AddCard(processedCards);
        battleManager.UpdateAllUI();
    }

    private void UpgradeAllPoisonCards()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager == null)
        {
            return;
        }

        List<Card> changedCards = new List<Card>();

        if (battleManager.handManager != null)
        {
            UpgradeCards(battleManager.handManager.GetHandCards(), changedCards);
            battleManager.handManager.RefreshCardDisplays(changedCards);
        }

        if (battleManager.usableDeckManager != null)
        {
            List<Card> drawPile = battleManager.usableDeckManager.GetDrawPile();
            if (UpgradeCards(drawPile, changedCards))
            {
                battleManager.usableDeckManager.SetDrawPile(drawPile);
            }

            UpgradeCards(battleManager.usableDeckManager.discardPile, changedCards);
        }

        battleManager.UpdateAllUI();
    }

    private bool UpgradeCards(IList<Card> cards, List<Card> changedCards)
    {
        if (cards == null || cards.Count == 0)
        {
            return false;
        }

        bool hasChanges = false;
        Card poisonPlusTemplate = CardManager.GetCardAsCard(BattleRuntimeDefinitions.PoisonPlusCardId);
        if (poisonPlusTemplate == null)
        {
            return false;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            if (card == null || card.cardId != BattleRuntimeDefinitions.PoisonCardId)
            {
                continue;
            }

            card.ApplyTemplate(poisonPlusTemplate);
            changedCards.Add(card);
            hasChanges = true;
        }

        return hasChanges;
    }

    private void ReducePlayerMaxHp(PlayerData target, int amount)
    {
        if (target == null || amount <= 0)
        {
            return;
        }

        target.maxHP = Mathf.Max(1, target.maxHP - amount);
        target.hp = Mathf.Min(target.hp, target.maxHP);

        if (TrainingRunState.IsRunActive)
        {
            TrainingRunState.SetPlayerHealthState(target.hp, target.maxHP);
        }

        TrainingBattleManager.Instance?.UpdateAllUI();
    }
}
