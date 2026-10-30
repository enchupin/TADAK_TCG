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
        VerifyCostDurations();
        return 4;
    }

    // 비용 변경 기간: 이번 턴 / 사용·버림 전까지 / 전투, 복제 시 영구 피해 증가 유지(R-22a)
    private static void VerifyCostDurations()
    {
        var card = new Card { cardId = 1, cost = 3 };
        card.InitializeRuntimeState();
        card.ApplyCostModifier(-1, "UntilPlayed");
        Assert(card.cost == 2, "사용 전까지 비용 감소");
        card.ClearTurnModifiers();
        Assert(card.cost == 2, "턴이 지나도 손에 있으면 유지");
        card.ApplyCostModifier(-1, "Turn");
        Assert(card.cost == 1, "이번 턴 감소와 중첩");
        card.ClearTurnModifiers();
        Assert(card.cost == 2, "이번 턴 감소만 해제");
        card.ClearUntilPlayedModifiers();
        Assert(card.cost == 3, "사용·버림 시 해제");
        card.SetCost(0, "UntilPlayed");
        Assert(card.cost == 0, "사용 전까지 비용 지정");
        card.ClearUntilPlayedModifiers();
        Assert(card.cost == 3, "비용 지정 해제");
        card.ApplyCostModifier(-1, (string)null);
        card.ClearUntilPlayedModifiers();
        card.ClearTurnModifiers();
        Assert(card.cost == 2, "기간 없는 감소는 전투 동안 유지");

        card.permanentDamageBonus = 6;
        card.sourceDeckIndex = 3;
        Card copy = card.CloneForRuntimeCopy();
        Assert(copy.permanentDamageBonus == 6 && copy.sourceDeckIndex == -1, "복제는 영구 피해 증가를 잇고 저장 덱 위치는 잇지 않음");
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
        var payloadEffect = new OrderEffect("fragment");
        var host = new Card { cardId = 700010, fragmentEffects = new() { payloadEffect }, fragmentDescription = "사용 시 피해를 4 입힙니다." };
        var hostEffect = new OrderEffect("host");
        host.effects.Add(hostEffect);
        Finish(CardEffectSequence.Run(host.PlaySequence(manager), manager));
        Assert(OrderEffect.Log.SequenceEqual(new[] { "host", "fragment" }), "조각은 본래 효과 다음에 발동");
        Assert(manager.battleContext.fragmentedCardsPlayedThisTurn == 1, "조각된 카드 사용 기록");
        Finish(CardEffectSequence.Run(host.PlaySequence(manager), manager));
        Assert(OrderEffect.Log.Count == 4, "조각은 이번 전투 동안 유지되어 다시 발동");
        Card copy = host.CloneForRuntimeCopy();
        Assert(copy.HasFragment && copy.fragmentEffects[0] == payloadEffect && copy.fragmentDescription == host.fragmentDescription, "복제 카드는 조각을 이어받음");
        Assert(FragmentEffect.ExtractQuotedText("조각: 손에서 카드를 1장 선택해 \"사용 시 보호막을 4 얻습니다.\"를 새깁니다.") == "사용 시 보호막을 4 얻습니다.", "조각 문구는 설명의 따옴표 안 문장");
        manager.battleContext.OnTurnStart();
        Assert(manager.battleContext.fragmentedCardsPlayedThisTurn == 0, "턴 시작 시 조각 사용 기록 초기화");
        OrderEffect.Log.Clear();

        // 조각 카드 사용: 손의 다른 카드에 효과와 따옴표 안 문구를 새김, 이미 조각된 카드는 제외
        manager = new TrainingBattleManager { PanelAvailable = false };
        var source = new Card { cardId = 302055, description = "조각: 손에서 카드를 1장 선택해 \"사용 시 피해를 4 입힙니다.\"를 새깁니다." };
        var engraved = new Card { cardId = 1, fragmentEffects = new() { payloadEffect } };
        var target = new Card { cardId = 2 };
        manager.handManager.Cards.AddRange(new[] { source, engraved, target });
        manager.battleContext.SetContextCards("ThisCard", new() { source });
        new FragmentEffect { payloadEffects = new() { payloadEffect } }.Execute(manager);
        Assert(target.HasFragment && target.fragmentDescription == "사용 시 피해를 4 입힙니다." && !source.HasFragment, "조각 효과와 문구를 다른 카드에 새김");
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
        // 강화는 카드 사용이 아니므로 손으로 되돌아오고 비용 · 사용 집계 없음
        Assert(!request.Accepted, "사용으로 수락하지 않아 드래그한 카드가 손으로 돌아감");
        Assert(manager.handManager.Cards.Count(c => c == card) == 1 && manager.usableDeckManager.Discard.Count == 0 && manager.usableDeckManager.Exhaust.Count == 0, "강화한 카드는 손에 남음");
        Assert(!manager.Ended && manager.handManager.Cards.Count(c => c != card) == 0, "그림자·종언 키워드 미적용");
        Assert(manager.battleContext.GetCardsPlayedThisCombatCount(null) == 0 && manager.IdentityCharges == 0 && manager.Payments == 0, "카드 사용으로 집계하지 않음");
        Assert(manager.PowerCalls == 0, "사용 시 발동 효과 미적용");
        Assert(!card.resolvedAsCuriosity && !card.isCurious, "처리 후 호기심 상태 해제");
        OrderEffect.Log.Clear();
        Console.WriteLine("PASS 호기심 강화: 사용 없이 강화하고 손으로");
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
