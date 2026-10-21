using System;
using static BattleRuntimeDefinitions;

// IMPLEMENTATION.md 5절 v1.0 신규 상태 효과 검증
static class BuffNewTests
{
    private static int assertions;

    public static int Run()
    {
        assertions = 0;
        VerifyMetadata();
        VerifyRoyal();
        VerifyExecutionerAndWater();
        VerifyHunterAndTrap();
        VerifyCatalyst();
        VerifyDefense();
        VerifyResources();
        VerifyOwlEyeAndCurse();
        VerifyCardBuffAmounts();
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

    private static void VerifyMetadata()
    {
        (int id, BuffPolarity polarity, bool stackable)[] specs =
        {
            (1024, BuffPolarity.Beneficial, false), (1025, BuffPolarity.Beneficial, false), (1026, BuffPolarity.Beneficial, false),
            (1027, BuffPolarity.Beneficial, false), (1028, BuffPolarity.Beneficial, false), (2003, BuffPolarity.Harmful, false),
            (4018, BuffPolarity.Harmful, true)
        };
        foreach (var (id, polarity, stackable) in specs)
        {
            Eq(BuffData.GetPolarity(id), polarity, $"신규 버프 {id} 극성");
            Eq(BuffData.IsNonStackableBuffId(id), !stackable, $"신규 버프 {id} 중첩");
        }

        for (int id = 3061; id <= 3075; id++)
        {
            Eq(BuffData.IsBeneficialBuffId(id) && !BuffData.IsNonStackableBuffId(id), true, $"신규 버프 {id}는 중첩 가능한 이로운 효과");
        }
    }

    private static Card Played(TrainingBattleManager battle, Character character, Monster target = null, int cardId = 1)
    {
        var card = new Card { cardId = cardId, character = character };
        battle.playerService.OnCardPlayed(card, target, false);
        return card;
    }

    private static void VerifyRoyal()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.energy = 0;
        player.AddBuff(RoyalBuffId, 1);
        battle.playerService.OnPlayerTurnStart();
        Played(battle, Character.Isla);
        Played(battle, Character.Isla);
        Played(battle, Character.Ignia);
        Eq(player.energy, 0, "두 명만 쓰면 로열 미발동");
        Played(battle, Character.Polar);
        Eq(player.energy, 1, "파티 3명의 카드를 모두 쓰면 에너지 1");
        Played(battle, Character.Monster);
        Played(battle, Character.Isla); Played(battle, Character.Ignia); Played(battle, Character.Polar);
        Eq(player.energy, 2, "다시 세 명을 채우면 재발동, 몬스터 카드는 제외");
        Played(battle, Character.Isla); Played(battle, Character.Ignia);
        battle.playerService.OnPlayerTurnStart();
        Played(battle, Character.Polar);
        Eq(player.energy, 2, "턴이 바뀌면 진행도 초기화");
    }

    private static void VerifyExecutionerAndWater()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.energy = 0;
        player.AddBuff(ExecutionerBuffId, 1);
        player.AddBuff(WaterEmbraceBuffId, 3);
        var plain = new Monster();
        battle.playerService.OnEnemyKilled(plain);
        Eq(player.energy, 0, "깃털 없는 적 처치는 처형자 미발동");
        var feathered = new Monster();
        feathered.AddBuff(FeatherBuffId, 1);
        battle.playerService.OnEnemyKilled(feathered);
        Eq(player.energy, 1, "깃털 보유 적 처치 시 에너지 1");
        Eq(battle.draws, 1, "깃털 보유 적 처치 시 카드 1장");
        Eq(player.GetBuffStack(WaveBuffId), 0, "익사가 아닌 처치는 물의 품 미발동");
        battle.playerService.MarkDrowningKill(plain);
        battle.playerService.OnEnemyKilled(plain);
        battle.playerService.ClearDrowningKill();
        Eq(player.GetBuffStack(WaveBuffId), 3, "익사 처치 시 물결");
    }

    private static void VerifyHunterAndTrap()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var monster = new Monster();
        battle.monsters.Add(monster);
        Eq(battle.Buffs.ResolveMonsterIncomingDamage(monster, 10), 10, "기본 피해");
        player.AddBuff(HunterBuffId, 1);
        Eq(battle.Buffs.ResolveMonsterIncomingDamage(monster, 10), 10, "기절·덫이 아니면 사냥꾼 미적용");
        monster.stunned = true;
        Eq(battle.Buffs.ResolveMonsterIncomingDamage(monster, 10), 15, "기절한 적은 관통 배율");
        monster.stunned = false;
        monster.AddBuff(TrapStateBuffId, 1);
        monster.AddBuff(CorrosionBuffId, 2);
        Eq(battle.Buffs.ResolveMonsterIncomingDamage(monster, 10), 15, "덫 상태 적은 부식 대신 관통 배율");
        monster.AddBuff(EnhancedCorrosionBuffId, 1);
        Eq(battle.Buffs.ResolveMonsterIncomingDamage(monster, 10), 15, "이미 관통이면 중복 적용 없음");
        Eq(battle.monsterService.ResolveBarrierGain(monster, 8), 0, "덫 상태는 보호막을 얻을 수 없음");
        battle.monsterService.OnMonsterTurnStart(monster);
        Eq(monster.GetBuffStack(TrapStateBuffId), 0, "덫 상태는 몬스터 다음 턴 시작 시 제거");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        var attacker = new Monster();
        battle.monsters.Add(attacker);
        player.AddBuff(TrapSetBuffId, 2);
        battle.playerService.OnEnemyAttackActionStarted(attacker);
        Eq(attacker.hp, 94, "덫 설치 피해 6");
        Eq(attacker.GetBuffStack(TrapStateBuffId), 1, "덫 설치로 덫 상태");
        Eq(player.GetBuffStack(TrapSetBuffId), 1, "덫 설치 1 소모");
    }

    private static void VerifyCatalyst()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var first = new Monster();
        var second = new Monster();
        battle.monsters.Add(first);
        battle.monsters.Add(second);
        first.AddBuff(BurnBuffId, 2);
        first.AddBuff(DrowningBuffId, 4);
        first.AddBuff(WeakBuffId, 1);
        second.AddBuff(FreezeBuffId, 1);
        player.AddBuff(CatalystBuffId, 3);
        Played(battle, Character.Isla, first, 1);
        Eq(player.GetBuffStack(CatalystBuffId), 3, "포션이 아니면 촉매 유지");
        Played(battle, Character.Isla, first, 101080);
        Eq(first.GetBuffStack(BurnBuffId), 5, "대상 발화 추가");
        Eq(first.GetBuffStack(DrowningBuffId), 7, "대상 익사 추가");
        Eq(first.GetBuffStack(WeakBuffId), 1, "반응 상태가 아닌 효과는 제외");
        Eq(first.GetBuffStack(FreezeBuffId), 0, "보유하지 않은 반응 상태는 부여 안 함");
        Eq(second.GetBuffStack(FreezeBuffId), 1, "대상이 있으면 다른 적 제외");
        Eq(player.GetBuffStack(CatalystBuffId), 0, "촉매 모두 소모");
        player.AddBuff(CatalystBuffId, 2);
        Played(battle, Character.Isla, null, 101080);
        Eq(second.GetBuffStack(FreezeBuffId), 3, "대상이 없으면 모든 적");
    }

    private static void VerifyDefense()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var attacker = new Monster();
        battle.monsters.Add(attacker);
        player.AddBuff(StealthBuffId, 2);
        Eq(player.TakeDamage(8, attacker), 6, "은신 받는 피해 25% 감소");
        Eq(player.GetBuffStack(StealthBuffId), 1, "피격마다 은신 1 감소");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        attacker = new Monster();
        player.AddBuff(SanctuaryBuffId, 3);
        Eq(player.TakeDamage(5, attacker), 2, "성역은 공격 피해를 타격마다 감소");
        Eq(player.TakeDamage(5), 5, "공격이 아닌 피해는 성역 미적용");
        battle.playerService.OnPlayerTurnStart();
        Eq(player.GetBuffStack(SanctuaryBuffId), 0, "성역은 다음 내 턴 시작 시 제거");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        attacker = new Monster();
        player.AddBuff(CounterBuffId, 4);
        player.defense = 10;
        player.TakeDamage(6, attacker);
        Eq(attacker.hp, 96, "보호막으로 완전히 막으면 반격");
        player.TakeDamage(10, attacker);
        Eq(attacker.hp, 96, "체력을 잃으면 반격 없음");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        attacker = new Monster();
        player.AddBuff(HibernationBuffId, 5);
        player.defense = 20;
        player.TakeDamage(12, attacker);
        player.TakeDamage(4);
        player.hp = 30;
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.hp, 36, "동면은 보호막이 막은 공격 피해의 50% 회복");
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.hp, 36, "회복 후 누적 초기화");
    }

    private static void VerifyResources()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var monster = new Monster();
        battle.monsters.Add(monster);
        player.AddBuff(EffectiveHitBuffId, 2);
        new AttackEffect { amount = 5, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ScoreBuffId), 2, "막히지 않은 공격마다 득점");
        new DamageEffect { amount = 5, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ScoreBuffId), 2, "공격이 아닌 피해는 유효타 제외");

        player.AddBuff(HighTideBuffId, 2);
        battle.playerService.OnPlayerTurnStart();
        Eq(player.GetBuffStack(WaveBuffId), 2, "만조 매 턴 물결");
        player.AddBuff(TideFlowBuffId, 3);
        Played(battle, Character.Mio, null, 204040);
        Played(battle, Character.Mio, null, 204030);
        Eq(player.GetBuffStack(WaveBuffId), 5, "팽창 카드 그룹만 조류 발동");

        player.AddBuff(BloodContractBuffId, 2);
        battle.Buffs.Player.OnPlayerCardHpLost(3);
        Eq(player.GetBuffStack(StrengthBuffId), 2, "카드로 체력을 잃으면 힘");
    }

    private static void VerifyOwlEyeAndCurse()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var monster = new Monster();
        battle.monsters.Add(monster);
        player.AddBuff(OwlEyeBuffId, 2);
        battle.ApplyBuffToMonster(monster, WeakBuffId, 1);
        Eq(monster.GetBuffStack(FeatherBuffId), 2, "해로운 효과 부여 시 깃털");
        battle.ApplyBuffToMonster(monster, FeatherBuffId, 1);
        Eq(monster.GetBuffStack(FeatherBuffId), 3, "깃털 부여 자체에는 반응하지 않음");

        player.AddBuff(CurseRecoveryBuffId, 4);
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.GetBuffStack(StrengthBuffId), 4, "저주 회복 턴 종료 시 힘");
        Eq(player.GetBuffStack(CurseRecoveryBuffId), 0, "저주 회복 제거");
    }

    // 카드 효과의 음수 부여(103070 시간 저주)와 강공 배율(201044)
    private static void VerifyCardBuffAmounts()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var monster = new Monster();
        battle.monsters.Add(monster);
        new BuffEffect { buffId = StrengthBuffId, amount = -6, target = TargetType.AllEnemies }.Execute(battle);
        Eq(monster.GetBuffStack(StrengthBuffId), -6, "음수 허용 효과는 카드에서 음수로 부여");
        new BuffEffect { buffId = WeakBuffId, amount = -2, target = TargetType.AllEnemies }.Execute(battle);
        Eq(monster.GetBuffStack(WeakBuffId), 1, "음수 비허용 효과는 기존처럼 최소 1 부여");

        battle = new TrainingBattleManager();
        player = battle.playerData;
        monster = new Monster();
        battle.monsters.Add(monster);
        var card = new Card();
        card.effects.Add(new AttackEffect { amount = 20, target = TargetType.SingleEnemy, boostMultiplier = 3 });
        player.AddBuff(AttackBoostBuffId, 4);
        battle.battleContext.SetContextCards("ThisCard", new() { card });
        card.effects[0].Execute(battle);
        Eq(monster.hp, 68, "강공 3배 적용");
        Eq(player.GetBuffStack(AttackBoostBuffId), 0, "강공 소모");
    }
}
