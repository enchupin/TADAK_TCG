using System;
using static BattleRuntimeDefinitions;

// IMPLEMENTATION.md 5절·3-3절 v1.0 변경 사항 검증
static class BuffRuleTests
{
    private static int assertions;

    public static int Run()
    {
        assertions = 0;
        VerifyDouble();
        VerifyStraight();
        VerifyCorrosion();
        VerifyTurnEffects();
        VerifyPolarity();
        return assertions;
    }

    private static void Eq<T>(T actual, T expected, string label)
    {
        assertions++;
        if (!Equals(actual, expected))
        {
            throw new Exception($"{label}: 기대 {expected}, 실제 {actual}");
        }
    }

    private static Card Played(TrainingBattleManager battle, int cost, int paidCost)
    {
        var card = new Card { cost = cost };
        battle.battleContext.RecordPaidCost(card, paidCost);
        battle.playerService.OnCardPlayed(card, null, false);
        return card;
    }

    private static void VerifyDouble()
    {
        var battle = new TrainingBattleManager();
        battle.playerData.AddBuff(DoubleJackBuffId, 1);
        battle.playerService.ResetForCombat();
        Played(battle, 1, 1);
        Eq(battle.draws, 0, "더블 첫 카드는 드로우 없음");
        Played(battle, 1, 1);
        Eq(battle.draws, 1, "같은 비용 연속 2장에 1장 드로우");
        Played(battle, 1, 1);
        Eq(battle.draws, 1, "발동 후 연속 횟수 초기화");
        Played(battle, 2, 2);
        Played(battle, 1, 1);
        Eq(battle.draws, 1, "다른 비용이 끼면 연속 끊김");
        Played(battle, 2, 1);
        Eq(battle.draws, 2, "할인 후 실제 비용으로 비교");
        battle.playerService.OnCardPlayed(new Card { cost = 1 }, null, true);
        Eq(battle.draws, 2, "반복 실행은 더블 집계 제외");
    }

    private static void VerifyStraight()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.energy = 0;
        player.AddBuff(StraightBuffId, 1);
        battle.playerService.OnPlayerTurnStart();
        Played(battle, 1, 1);
        Played(battle, 3, 3);
        Eq(battle.draws, 0, "비용 2 없이는 스트레이트 미발동");
        Played(battle, 2, 2);
        Eq(player.energy, 2, "스트레이트 에너지 2");
        Eq(battle.draws, 1, "스트레이트 카드 1장");
        Played(battle, 1, 1); Played(battle, 2, 2); Played(battle, 3, 3);
        Eq(battle.draws, 1, "스트레이트 턴당 1회");
        battle.playerService.OnPlayerTurnStart();
        Played(battle, 3, 3); Played(battle, 2, 2); Played(battle, 1, 0);
        Eq(battle.draws, 1, "실제 비용 0이면 비용 1 조건 미충족");
        Played(battle, 1, 1);
        Eq(battle.draws, 2, "다음 턴 다시 발동");
    }

    private static void VerifyCorrosion()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.AddBuff(CorrosionBuffId, 2);
        Eq(battle.playerService.GetIncomingDamageMultiplier(null), 1.25f, "부식 25% 증가");
        Eq(player.TakeDamage(8), 10, "부식은 모든 피격에 적용");
        Eq(player.TakeDamage(8), 10, "부식은 피격으로 소모되지 않음");
        Eq(player.GetBuffStack(CorrosionBuffId), 2, "피격 후 부식 유지");
        battle.playerService.OnPlayerTurnStart();
        Eq(player.GetBuffStack(CorrosionBuffId), 1, "보유자 턴 시작 시 1 감소");
        player.AddBuff(EnhancedCorrosionBuffId, 2);
        Eq(battle.playerService.GetIncomingDamageMultiplier(null), 1.5f, "관통 우선 적용, 부식 미중첩");
        battle.playerService.OnPlayerTurnStart();
        Eq(player.GetBuffStack(EnhancedCorrosionBuffId), 1, "관통 턴 시작 감소");
        Eq(player.GetBuffStack(CorrosionBuffId), 1, "관통 보유 중 부식은 감소하지 않음");
        battle.playerService.OnPlayerTurnStart();
        Eq(player.GetBuffStack(EnhancedCorrosionBuffId), 0, "마지막 관통 감소");
        Eq(player.GetBuffStack(CorrosionBuffId), 1, "턴 시작 시점에 관통이 있었다면 부식 유지");

        var monster = new Monster();
        battle.monsters.Add(monster);
        monster.AddBuff(CorrosionBuffId, 2);
        Eq(battle.monsterService.GetIncomingDamageMultiplier(monster), 1.25f, "몬스터 부식 25%");
        player.AddBuff(CorrosionEnhanceBuffId, 1);
        Eq(battle.monsterService.GetIncomingDamageMultiplier(monster), 1.5f, "강화: 부식 보유 중 적 부식은 관통 배율");
        battle.ApplyBuffToMonster(monster, CorrosionBuffId, 1);
        Eq(monster.GetBuffStack(CorrosionBuffId), 3, "강화: 부식은 부여 시 관통으로 바꾸지 않음");
        Eq(monster.GetBuffStack(EnhancedCorrosionBuffId), 0, "부여로 관통이 생기지 않음");
        battle.monsterService.TryConsumeIncomingDamageBuff(monster);
        Eq(monster.GetBuffStack(CorrosionBuffId), 3, "몬스터 부식은 피격으로 소모되지 않음");
        battle.monsterService.OnMonsterTurnStart(monster);
        Eq(monster.GetBuffStack(CorrosionBuffId), 2, "몬스터 턴 시작 시 부식 감소");
        monster.AddBuff(EnhancedCorrosionBuffId, 1);
        battle.monsterService.OnMonsterTurnStart(monster);
        Eq(monster.GetBuffStack(CorrosionBuffId), 2, "관통 보유 몬스터의 부식은 감소하지 않음");
        Eq(monster.GetBuffStack(EnhancedCorrosionBuffId), 0, "몬스터 관통 감소");
    }

    private static void VerifyTurnEffects()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.AddBuff(BurningWillBuffId, 4);
        battle.playerService.OnPlayerTurnStart();
        Eq(player.hp, 48, "불타는 투지 체력 2 손실");
        Eq(player.defense, 4, "불타는 투지 보호막");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        player.AddBuff(IndomitableBuffId, 3);
        battle.SetState(BattleTurnState.PlayerAction);
        player.LoseHp(2);
        Eq(player.defense, 3, "불굴은 내 턴 체력 손실 시 보호막");
        Eq(player.GetBuffStack(StrengthBuffId), 0, "불굴은 힘을 주지 않음");
        battle.SetState(BattleTurnState.EnemyAction);
        player.LoseHp(2);
        Eq(player.defense, 3, "적 턴 체력 손실은 불굴 미발동");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        player.AddBuff(OverheatGrowthBuffId, 3);
        battle.playerService.OnPlayerTurnStart();
        Eq(player.GetBuffStack(AttackBoostBuffId), 3, "발열은 매 턴 시작 시 강공");

        player.AddBuff(FlameConductionBuffId, 2);
        var monster = new Monster();
        battle.monsters.Add(monster);
        battle.playerService.OnPlayerAttackResolved(monster, 0, 0);
        Eq(monster.GetBuffStack(BurnBuffId), 2, "불주먹 공격 시 발화");
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.GetBuffStack(FlameConductionBuffId), 0, "불주먹은 턴 종료 시 제거");

        player.AddBuff(DamageClampToOneBuffId, 2);
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.GetBuffStack(DamageClampToOneBuffId), 2, "아슬아슬은 내 턴 종료에 감소하지 않음");
        battle.playerService.OnEnemyTurnEnd();
        Eq(player.GetBuffStack(DamageClampToOneBuffId), 1, "아슬아슬은 적 턴 종료 시 1 감소");

        player.AddBuff(DelayedPotionFactoryBuffId, 2);
        battle.playerService.OnPlayerTurnStart();
        Eq(battle.handManager.cards.Count, 2, "제조 매 턴 포션 생성");
        Eq(player.GetBuffStack(DelayedPotionFactoryBuffId), 2, "제조 중첩 유지");
        battle.duplicate = true;
        battle.playerService.OnPlayerTurnStart();
        Eq(battle.handManager.cards.Count, 4, "상태 효과 생성은 복제 생성 미적용");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        player.AddBuff(GlacierBondBuffId, 6);
        player.AddBuff(4015, 2);
        battle.playerService.ReplayTurnEndTriggeredEffects();
        battle.playerService.ReplayTurnEndTriggeredEffects();
        Eq(player.GetBuffStack(GlacierBondBuffId), 2, "해체는 이중 동작으로 반복");
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.GetBuffStack(4015), 0, "해체는 턴 종료 처리 후 제거");
    }

    private static void VerifyPolarity()
    {
        var battle = new TrainingBattleManager();
        var monster = new Monster();
        battle.monsters.Add(monster);
        battle.ApplyBuffToMonster(monster, MirrorBuffId, 3);
        Eq(battle.battleContext.EnemyDebuffNotifications, 0, "패시브 부여는 해로운 효과 알림 제외");
        battle.ApplyBuffToMonster(monster, WhirlpoolBuffId, 2);
        Eq(battle.battleContext.EnemyDebuffNotifications, 1, "4017 소용돌이는 해로운 효과 알림");
    }
}
