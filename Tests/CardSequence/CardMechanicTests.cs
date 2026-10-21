using System;
using System.Collections;
using System.Linq;

// 4단계 카드 메커니즘: 동전(R-72), 조각(R-73), 호기심 사용 처리(R-21·R-22, E-12)
static class CardMechanicTests
{
    public static int Run()
    {
        VerifyCoin();
        VerifyFragment();
        VerifyCuriosityPlay();
        return 3;
    }

    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void VerifyCoin()
    {
        var context = new BattleContext();
        context.SetCoinSeed(42);
        var card = new Card { cardId = 1 };
        bool first = context.ResolveCoinFlip(card, () => false);
        for (int i = 0; i < 5; i++)
        {
            Assert(context.ResolveCoinFlip(card, () => false) == first, "같은 카드 사용 안의 동전은 같은 결과");
        }

        int luckChecks = 0;
        context.ResetCoinFlip(card);
        Assert(context.ResolveCoinFlip(card, () => { luckChecks++; return true; }), "행운이 있으면 앞면 확정");
        Assert(luckChecks == 1, "행운은 동전을 던질 때 한 번만 소모");
        context.ResolveCoinFlip(card, () => { luckChecks++; return true; });
        Assert(luckChecks == 1, "이미 던진 동전은 행운을 다시 소모하지 않음");

        var seeded = new BattleContext();
        seeded.SetCoinSeed(7);
        var replay = new BattleContext();
        replay.SetCoinSeed(7);
        for (int i = 0; i < 20; i++)
        {
            var flipCard = new Card { cardId = i };
            Assert(seeded.ResolveCoinFlip(flipCard, () => false) == replay.ResolveCoinFlip(flipCard, () => false), "같은 시드는 같은 동전 순서");
        }

        int heads = 0;
        var fair = new BattleContext();
        fair.SetCoinSeed(1);
        for (int i = 0; i < 1000; i++) if (fair.ResolveCoinFlip(new Card(), () => false)) heads++;
        Assert(heads > 400 && heads < 600, "동전은 50% 확률");
        Console.WriteLine("PASS 동전: 사용당 1회, 행운 확정, 시드 재현");
    }

    private static void VerifyFragment()
    {
        var manager = new TrainingBattleManager();
        var payload = new Card { cardId = 900001 };
        var payloadEffect = new OrderEffect("fragment");
        payload.effects.Add(payloadEffect);
        CardManager.Cards[900001] = payload;

        var host = new Card { cardId = 700010, fragmentCardId = 900001 };
        var hostEffect = new OrderEffect("host");
        host.effects.Add(hostEffect);
        Finish(CardEffectSequence.Run(host.PlaySequence(manager), manager));
        Assert(OrderEffect.Log.SequenceEqual(new[] { "host", "fragment" }), "조각은 본래 효과 다음에 발동");
        Assert(manager.battleContext.fragmentedCardsPlayedThisTurn == 1, "조각된 카드 사용 기록");
        Finish(CardEffectSequence.Run(host.PlaySequence(manager), manager));
        Assert(OrderEffect.Log.Count == 4, "조각은 이번 전투 동안 유지되어 다시 발동");
        Assert(host.CloneForRuntimeCopy().fragmentCardId == 900001, "복제 카드는 조각을 이어받음");
        manager.battleContext.OnTurnStart();
        Assert(manager.battleContext.fragmentedCardsPlayedThisTurn == 0, "턴 시작 시 조각 사용 기록 초기화");
        CardManager.Cards.Remove(900001);
        OrderEffect.Log.Clear();
        Console.WriteLine("PASS 조각: 본래 효과 후 발동, 전투 동안 유지");
    }

    private static void VerifyCuriosityPlay()
    {
        var manager = new TrainingBattleManager { Repeats = 2 };
        var card = new Card { cardId = 700020, isCurious = true, sourceDeckIndex = 0 };
        card.AddKeyword(CardKeywordIds.Exhaust);
        card.AddKeyword(CardKeywordIds.Shadow);
        card.AddKeyword(CardKeywordIds.Finale);
        var effect = new OrderEffect("original");
        card.effects.Add(effect);
        manager.handManager.AddCard(card);

        var resolver = new CombatResolver(manager);
        var request = new CardPlayEventData { cardController = new CardController { Card = card, cardUI = new CardUI { Card = card } } };
        resolver.TryPlayCard(request);
        for (int i = 0; i < 20 && resolver.IsResolvingCardPlay; i++) manager.Tick();

        Assert(manager.CuriosityPlays == 1, "호기심 카드는 강화 절차 1회");
        Assert(OrderEffect.Log.Count == 0, "호기심 카드는 본래 효과를 발동하지 않음");
        Assert(manager.Repeats == 2, "호기심 사용은 재사용을 소모하지 않음");
        Assert(manager.usableDeckManager.Discard.Count(c => c == card) == 1 && manager.usableDeckManager.Exhaust.Count == 0, "소멸 키워드와 무관하게 버린 더미로 이동");
        Assert(!manager.Ended && manager.handManager.Cards.Count(c => c != card) == 0, "그림자·종언 키워드 미적용");
        Assert(manager.battleContext.GetCardsPlayedThisCombatCount(null) == 1 && manager.IdentityCharges == 1, "카드 사용으로 집계");
        Assert(!card.resolvedAsCuriosity && !card.isCurious, "처리 후 호기심 상태 해제");
        OrderEffect.Log.Clear();
        Console.WriteLine("PASS 호기심 사용: 강화만 진행하고 버린 더미로");
    }

    private static void Finish(IEnumerator sequence)
    {
        for (int i = 0; i < 20; i++) if (!sequence.MoveNext()) return;
        throw new Exception("카드 실행이 종료되지 않음");
    }

    private sealed class OrderEffect : ICardEffect
    {
        public static readonly System.Collections.Generic.List<string> Log = new();
        private readonly string name;
        public OrderEffect(string name) { this.name = name; }
        public void Execute(TrainingBattleManager manager) => Log.Add(name);
    }
}
