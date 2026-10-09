using System;
using System.Collections;
using System.Linq;

static class CardCompletionTests
{
    public static int Run()
    {
        int passed = 0;
        foreach (bool triggered in new[] { false, true })
        {
            foreach (string destination in new[] { "discard", "exhaust", "power", "potion", "unlocked", "moved" })
            {
                VerifyDestination(triggered, destination);
                passed++;
            }
            VerifyKeywordsAndRepeats(triggered);
            passed++;
        }
        VerifyEffectOnlyExecution();
        VerifyNullCompletion();
        return passed + 2;
    }

    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void VerifyDestination(bool triggered, string destination)
    {
        var manager = new TrainingBattleManager();
        var card = new Card { cardId = 700001 };
        manager.handManager.AddCard(card);
        if (destination == "exhaust" || destination == "potion") card.AddKeyword(CardKeywordIds.Exhaust);
        if (destination == "power" || destination == "exhaust") card.AddKeyword(CardKeywordIds.Power);
        manager.PotionGoesToDiscard = destination == "potion";
        manager.ExhaustUnlocked = destination == "unlocked";
        if (destination == "moved") card.effects.Add(new MoveToDrawEffect());

        Play(manager, card, triggered);
        int discarded = manager.usableDeckManager.Discard.Count(c => c == card);
        int exhausted = manager.usableDeckManager.Exhaust.Count(c => c == card);
        int drawn = manager.usableDeckManager.Draw.Count(c => c == card);
        Assert(discarded == (destination == "discard" || destination == "potion" ? 1 : 0), "행선지 우선순위와 버리기 한 번 처리");
        Assert(exhausted == (destination == "exhaust" || destination == "unlocked" ? 1 : 0), "파워보다 소멸 우선, 사용 불가 해제 소멸 유지");
        Assert(drawn == (destination == "moved" ? 1 : 0), "효과에서 이미 이동한 카드는 기본 버리기 제외");
        Assert(manager.RemovedFromCombat == (destination == "power" ? 1 : 0), "파워는 전투에서 이탈");
        Console.WriteLine($"PASS {(triggered ? "유발" : "직접")} 사용 행선지: {destination}");
    }

    private static void VerifyKeywordsAndRepeats(bool triggered)
    {
        var manager = new TrainingBattleManager { Repeats = 2 };
        var card = new Card { cardId = 700002, cost = 3 };
        card.AddKeyword(CardKeywordIds.Shadow);
        card.AddKeyword(CardKeywordIds.Finale);
        var effect = new CountEffect();
        card.effects.Add(effect);
        manager.handManager.AddCard(card);

        Play(manager, card, triggered);
        Assert(effect.Count == 3 && manager.PowerCalls == 3, "최초 실행과 두 번 반복마다 사용 후 훅 실행");
        Assert(manager.battleContext.GetCardsPlayedThisCombatCount(null) == 1, "반복은 카드 사용 통계를 늘리지 않음");
        Assert(manager.Payments == (triggered ? 0 : 1), "직접 사용만 비용을 한 번 지불");
        Assert(manager.IdentityCharges == (triggered ? 0 : 1), "직접 사용만 아이덴티티를 한 번 충전");
        var copies = manager.handManager.Cards.Where(c => c != card).ToList();
        Assert(copies.Count == 1 && copies[0].cost == 2 && copies[0].HasKeyword(CardKeywordIds.Ghost), "그림자는 모든 반복 뒤 유령 카드 한 장 생성");
        Assert(manager.Ended && manager.ForceEndCalls == 1, "피날레는 모든 반복 뒤 한 번 처리");
        Assert(manager.usableDeckManager.Discard.Count(c => c == card) == 1, "카드 행선지는 반복 뒤 한 번 처리");
        Console.WriteLine($"PASS {(triggered ? "유발" : "직접")} 사용: 반복·비용·통계·그림자·피날레");
    }

    private static void VerifyEffectOnlyExecution()
    {
        var manager = new TrainingBattleManager { Repeats = 2 };
        var outerCard = new Card();
        var card = new Card();
        card.AddKeyword(CardKeywordIds.Exhaust);
        card.AddKeyword(CardKeywordIds.Shadow);
        card.AddKeyword(CardKeywordIds.Finale);
        var effect = new CountEffect();
        card.effects.Add(effect);
        var target = new Monster();
        manager.currentTarget = target;
        manager.battleContext.SetContextCards("ThisCard", new() { outerCard });

        Finish(CardEffectSequence.Run(TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(manager, card), manager));
        Assert(effect.Count == 1 && manager.Repeats == 2, "옵션 없는 유발 실행은 재사용을 소모하지 않음");
        Assert(manager.PowerCalls == 0 && !manager.Ended && manager.handManager.Cards.Count == 0, "옵션 없는 유발 실행은 사용 후 훅과 키워드 제외");
        Assert(manager.usableDeckManager.Exhaust.Count == 0 && manager.usableDeckManager.Discard.Count == 0, "옵션 없는 유발 실행은 행선지 제외");
        Assert(manager.currentTarget == target && manager.battleContext.GetContextCard("ThisCard") == outerCard, "유발 실행 후 외부 대상과 카드 문맥 복구");
        Console.WriteLine("PASS 유발 실행 옵션 비활성 및 외부 문맥 복구");
    }

    private static void VerifyNullCompletion()
    {
        CardPlayCompletion.ResolveDestination(null, null);
        CardPlayCompletion.ApplyKeywords(null, null);
        var manager = new TrainingBattleManager();
        CardPlayCompletion.ResolveDestination(manager, null);
        CardPlayCompletion.ApplyKeywords(manager, null);
        Assert(!manager.Ended && manager.usableDeckManager.Discard.Count == 0, "없는 카드의 후처리는 상태를 바꾸지 않음");
        Console.WriteLine("PASS 비어 있는 카드 후처리");
    }

    private static void Play(TrainingBattleManager manager, Card card, bool triggered)
    {
        if (triggered)
        {
            Finish(CardEffectSequence.Run(TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(
                manager, card, resolveDestination: true, triggerPowerEffects: true,
                allowRepeats: true, applyPostPlayKeywords: true), manager));
            return;
        }

        var resolver = new CombatResolver(manager);
        var request = new CardPlayEventData
        {
            cardController = new CardController { Card = card, cardUI = new CardUI { Card = card } }
        };
        resolver.TryPlayCard(request);
        for (int i = 0; i < 20 && resolver.IsResolvingCardPlay; i++) manager.Tick();
        Assert(request.Accepted && !resolver.IsResolvingCardPlay, "직접 사용이 승인되고 종료됨");
    }

    private static void Finish(IEnumerator sequence)
    {
        for (int i = 0; i < 20; i++) if (!sequence.MoveNext()) return;
        throw new Exception("카드 실행이 종료되지 않음");
    }

    private sealed class CountEffect : ICardEffect
    {
        public int Count;
        public void Execute(TrainingBattleManager manager) => Count++;
    }

    private sealed class MoveToDrawEffect : ICardEffect
    {
        public void Execute(TrainingBattleManager manager)
        {
            manager.usableDeckManager.AddToDrawPileTop(manager.battleContext.GetContextCard("ThisCard"));
        }
    }
}
