using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

static class Program
{
    static int passed;
    static readonly Dictionary<int, JsonElement> definitions = new();
    static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    static void Test(string name, Action action)
    {
        action(); passed++; Console.WriteLine($"PASS {name}");
    }
    static Card Load(int id)
    {
        var data = definitions[id];
        var select = data.GetProperty("effects")[0];
        var next = data.GetProperty("effects")[1];
        string subject = select.GetProperty("subject").GetString();
        int count = select.TryGetProperty("count", out var n) ? n.GetInt32() : select.GetProperty("amount").GetInt32();
        Assert(select.GetProperty("type").GetString() == "SelectCard", "선택 효과 정의");
        Assert(select.GetProperty("onAction").GetProperty("type").GetString() == "Move", "버리기 효과 정의");
        var card = new Card { cardId = id, character = (Character)data.GetProperty("characterId").GetInt32() };
        card.effects.Add(new SelectCardEffect {
            count = count, from = MoveZoneType.Hand,
            onActions = new() { new MoveEffect { from = MoveZoneType.Source, to = MoveZoneType.DiscardPile, subject = subject } }
        });
        if (next.GetProperty("type").GetString() == "Draw")
            card.effects.Add(new DrawEffect { amount = next.GetProperty("amount").GetInt32() });
        else
        {
            Assert(next.GetProperty("type").GetString() == "RandGenerate", "생성 효과 정의");
            card.effects.Add(new RandGenerateEffect { amount = next.GetProperty("amount").GetInt32(), to = MoveZoneType.Hand, cardIdList = new() { 101091 } });
        }
        return card;
    }
    static TrainingBattleManager Setup(Card card, int handSize = 10)
    {
        var m = new TrainingBattleManager();
        m.handManager.Cards.Add(card);
        for (int i = 1; i < handSize; i++) m.handManager.Cards.Add(new Card { cardId = i });
        for (int i = 0; i < 20; i++) m.usableDeckManager.Draw.Add(new Card { cardId = 900 + i });
        m.battleContext.OnCardPlayed(card);
        return m;
    }
    static IEnumerator Run(Card card, TrainingBattleManager manager) => CardEffectSequence.Run(card.PlaySequence(manager), manager);
    static void Finish(IEnumerator sequence)
    {
        for (int i = 0; i < 20; i++) if (!sequence.MoveNext()) return;
        throw new Exception("효과 실행이 종료되지 않음");
    }
    static CardPlayEventData Event(Card card, Monster target = null) => new() {
        cardController = new CardController { Card = card, cardUI = new CardUI { Card = card } }, targetMonster = target
    };

    static void Main(string[] args)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(args[0], "Assets/Resources/JsonData"), "*Cards.json"))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var card in json.RootElement.GetProperty("cards").EnumerateArray())
                definitions[card.GetProperty("cardId").GetInt32()] = card.Clone();
        }
        foreach (int id in new[] { 101044, 102053, 202042 })
        {
            Test($"{id}: 선택 확정 전 대기, 가득 찬 손패에서 버린 뒤 보충", () => {
                var card = Load(id); var m = Setup(card); var run = Run(card, m);
                Assert(run.MoveNext(), "선택 대기");
                Assert(m.DrawCalls == 0 && m.Generated == 0 && m.usableDeckManager.Discard.Count == 0, "미확정 효과 실행 금지");
                Assert(m.battleContext.GetContextCard("ThisCard") == card, "카드 컨텍스트 유지");
                Assert(!m.Candidates.Contains(card), "사용 중인 카드 제외");
                Assert(run.MoveNext() && m.DrawCalls == 0 && m.Generated == 0, "다음 프레임도 대기");
                int selectedCount = m.RequiredCount;
                m.Confirm(); Finish(run);
                Assert(m.usableDeckManager.Discard.Count == selectedCount, "선택한 카드 모두 버리기");
                Assert(m.handManager.Cards.Count == 10, "버리기로 생긴 손패 공간 보충");
                Assert(id == 101044 ? m.Generated == 2 : m.battleContext.cardsDrawnThisTurn == 2, "후속 효과 정확히 한 번 실행");
                Assert(m.battleContext.GetSelectedCards().Count == 0 && m.battleContext.GetContextCard("ThisCard") == null, "컨텍스트 정리");
            });
        }
        Test("선택할 카드가 없으면 후속 효과 진행", () => {
            var c = Load(102053); var m = Setup(c, 1); Finish(Run(c, m));
            Assert(m.OpenCount == 0 && m.battleContext.cardsDrawnThisTurn == 2, "빈 후보 처리");
        });
        Test("후보 부족 시 가능한 수만 선택", () => {
            var c = Load(102053); var m = Setup(c, 2); var r = Run(c, m); r.MoveNext();
            Assert(m.RequiredCount == 1, "선택 수 제한"); m.Confirm(); Finish(r);
            Assert(m.usableDeckManager.Discard.Count == 1 && m.DrawCalls == 1, "후속 효과 처리");
        });
        foreach (string mode in new[] { "random", "fallback", "immediate" })
            Test($"동기 선택 경로: {mode}", () => {
                var c = Load(102053); var m = Setup(c);
                ((SelectCardEffect)c.effects[0]).random = mode == "random";
                m.PanelAvailable = mode != "fallback"; m.ImmediateSelection = mode == "immediate";
                Finish(Run(c, m));
                Assert(m.usableDeckManager.Discard.Count == 2 && m.battleContext.cardsDrawnThisTurn == 2, "동기 실행 순서");
            });
        Test("0장 선택 확정", () => {
            var c = Load(102053); var m = Setup(c, 3); ((SelectCardEffect)c.effects[0]).allowFewerSelection = true;
            var r = Run(c, m); r.MoveNext(); m.Confirm(new()); Finish(r);
            Assert(m.usableDeckManager.Discard.Count == 0 && m.DrawCalls == 1, "0장 선택 후 진행");
        });
        Test("중첩 선택 완료 후 외부 선택 컨텍스트 복구", () => {
            var c = Load(102053); var m = Setup(c); var outer = (SelectCardEffect)c.effects[0];
            Card outerSelection = m.handManager.Cards[1];
            outer.onActions.Insert(0, new SelectCardEffect { from = MoveZoneType.Hand, count = 1 });
            var r = Run(c, m); r.MoveNext(); m.Confirm(new() { outerSelection }); r.MoveNext();
            Assert(m.OpenCount == 2 && m.DrawCalls == 0, "중첩 선택 대기");
            m.Confirm(new() { m.handManager.Cards[2] }); Finish(r);
            Assert(m.usableDeckManager.Discard.Single() == outerSelection, "외부 선택 복구");
        });
        Test("조건 및 반복 효과 내부 선택 순서", () => {
            var c = Load(102053); var select = c.effects[0];
            c.effects[0] = new RepeatEffect { amount = 2, effectToRepeat = new ConditionalEffect { successEffects = new() { select } } };
            var m = Setup(c); var r = Run(c, m); r.MoveNext(); m.Confirm(); r.MoveNext();
            Assert(m.OpenCount == 2 && m.DrawCalls == 0, "두 번째 선택 전 드로우 금지");
            m.Confirm(); Finish(r); Assert(m.DrawCalls == 1, "반복 완료 후 드로우");
        });
        Test("일반 카드 사용: 대상 유지, 중복 사용 차단, 반복 선택 직렬 처리", () => {
            var c = Load(102053); var m = Setup(c); var target = new Monster(); m.Repeats = 1;
            var resolver = new CombatResolver(m); resolver.TryPlayCard(Event(c, target));
            Assert(resolver.IsResolvingCardPlay && m.currentTarget == target && m.PowerCalls == 0, "선택 중 상태 유지");
            var rejected = Event(c); resolver.TryPlayCard(rejected); Assert(!rejected.Accepted, "중복 사용 차단");
            m.Confirm(); m.Tick(); Assert(m.OpenCount == 2 && m.DrawCalls == 1 && resolver.IsResolvingCardPlay, "첫 사용 완료 뒤 반복");
            m.Confirm(); m.Tick(); m.Tick();
            Assert(!resolver.IsResolvingCardPlay && m.currentTarget == null && m.DrawCalls == 2, "반복 완료 후 잠금 해제");
            Assert(m.usableDeckManager.Discard.Contains(c), "모든 효과 완료 후 카드 이동");
        });
        Test("덱 자동 사용: 선택을 마친 뒤 다음 카드 사용", () => {
            var first = Load(102053); var parent = new Card(); var m = Setup(parent, 5);
            var next = new Card(); int playedNext = 0; next.effects.Add(new ActionEffect(_ => playedNext++));
            m.usableDeckManager.Draw.Insert(0, next); m.usableDeckManager.Draw.Insert(0, first);
            first.effects.RemoveAt(1);
            parent.effects.Add(new UseTopDeckCardsEffect { amount = 2 });
            var r = Run(parent, m); r.MoveNext(); Assert(playedNext == 0 && !m.usableDeckManager.Discard.Contains(first), "자동 사용 대기");
            m.Confirm(); Finish(r); Assert(playedNext == 1 && m.usableDeckManager.Discard.Contains(first), "자동 사용 완료");
        });
        Test("바인딩된 카드의 선택도 완료 대기", () => {
            var parent = new Card(); var child = Load(102053); CardManager.Cards[child.cardId] = child;
            parent.boundCardIds.Add(child.cardId); var m = Setup(parent, 5); var r = Run(parent, m);
            r.MoveNext(); Assert(m.battleContext.GetContextCard("ThisCard") == child, "바인딩 카드 컨텍스트");
            m.Confirm(); Finish(r); Assert(m.DrawCalls == 1 && m.battleContext.GetContextCard("ThisCard") == null, "부모 종료");
        });
        Test("효과 예외 발생 시 컨텍스트와 입력 잠금 복구", () => {
            var c = Load(102053); c.effects.Add(new ActionEffect(_ => throw new InvalidOperationException("검증 예외")));
            var m = Setup(c); var resolver = new CombatResolver(m); resolver.TryPlayCard(Event(c, new Monster())); m.Confirm();
            bool failed = false; try { m.Tick(); } catch (InvalidOperationException) { failed = true; }
            Assert(failed && !resolver.IsResolvingCardPlay && m.currentTarget == null && m.battleContext.GetContextCard("ThisCard") == null, "예외 복구");
        });
        Test("선택 중 전투 종료 시 후속 효과 중단", () => {
            var c = Load(102053); var m = Setup(c); var resolver = new CombatResolver(m); resolver.TryPlayCard(Event(c));
            m.CurrentTurnState = BattleTurnState.CombatEnd; m.Tick(); m.Confirm(); m.Tick();
            Assert(!resolver.IsResolvingCardPlay && m.DrawCalls == 0 && m.battleContext.GetContextCard("ThisCard") == null, "종료 후 효과 실행 금지");
        });
        Test("선택 없는 카드 효과는 첫 프레임에 완료", () => {
            var c = new Card(); int count = 0; c.effects.Add(new ActionEffect(_ => count++)); var m = Setup(c);
            c.Play(m); Assert(count == 1 && m.battleContext.GetContextCard("ThisCard") == null, "동기 효과 호환");
        });
        passed += CardCompletionTests.Run();
        Console.WriteLine($"{passed} tests passed");
    }
    sealed class ActionEffect : ICardEffect
    {
        readonly Action<TrainingBattleManager> action;
        public ActionEffect(Action<TrainingBattleManager> action) => this.action = action;
        public void Execute(TrainingBattleManager manager) => action(manager);
    }
}
