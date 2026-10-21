using System;
using static BattleRuntimeDefinitions;

static class BuffStackTests
{
    private static int assertions;

    public static int Run()
    {
        assertions = 0;
        VerifyStackRules();
        VerifyRepeatCharges();
        VerifyComboDamage();
        VerifyComboTargets();
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

    // IMPLEMENTATION.md 5절 표의 극성과 중첩 규칙 (v1.0에 남은 기존 효과)
    private static readonly (int id, BuffPolarity polarity, bool stackable)[] SpecBuffs =
    {
        (1001, BuffPolarity.Beneficial, false), (1002, BuffPolarity.Beneficial, false), (1003, BuffPolarity.Beneficial, false),
        (1004, BuffPolarity.Beneficial, false), (1006, BuffPolarity.Beneficial, false), (1007, BuffPolarity.Beneficial, false),
        (1008, BuffPolarity.Beneficial, false), (1009, BuffPolarity.Beneficial, false), (1010, BuffPolarity.Beneficial, false),
        (1013, BuffPolarity.Beneficial, false), (1014, BuffPolarity.Beneficial, false), (1015, BuffPolarity.Beneficial, false),
        (1017, BuffPolarity.Beneficial, false), (1018, BuffPolarity.Beneficial, false), (1019, BuffPolarity.Beneficial, false),
        (1020, BuffPolarity.Beneficial, false), (1021, BuffPolarity.Beneficial, false), (1022, BuffPolarity.Beneficial, false),
        (2001, BuffPolarity.Harmful, false), (2002, BuffPolarity.Harmful, false),
        (3001, BuffPolarity.Beneficial, true), (3002, BuffPolarity.Beneficial, true), (3003, BuffPolarity.Beneficial, true),
        (3004, BuffPolarity.Beneficial, true), (3006, BuffPolarity.Beneficial, true), (3008, BuffPolarity.Beneficial, true),
        (3009, BuffPolarity.Beneficial, true), (3010, BuffPolarity.Beneficial, true), (3011, BuffPolarity.Beneficial, true),
        (3012, BuffPolarity.Beneficial, true), (3013, BuffPolarity.Beneficial, true), (3014, BuffPolarity.Beneficial, true),
        (3015, BuffPolarity.Beneficial, true), (3016, BuffPolarity.Beneficial, true), (3018, BuffPolarity.Beneficial, true),
        (3019, BuffPolarity.Beneficial, true), (3020, BuffPolarity.Beneficial, true), (3023, BuffPolarity.Beneficial, true),
        (3024, BuffPolarity.Beneficial, true), (3026, BuffPolarity.Beneficial, true), (3027, BuffPolarity.Beneficial, true),
        (3028, BuffPolarity.Beneficial, true), (3029, BuffPolarity.Beneficial, true), (3031, BuffPolarity.Beneficial, true),
        (3032, BuffPolarity.Beneficial, true), (3034, BuffPolarity.Beneficial, true), (3035, BuffPolarity.Beneficial, true),
        (3036, BuffPolarity.Beneficial, true), (3037, BuffPolarity.Beneficial, true), (3041, BuffPolarity.Beneficial, true),
        (3042, BuffPolarity.Beneficial, true), (3043, BuffPolarity.Beneficial, true), (3044, BuffPolarity.Beneficial, true),
        (3045, BuffPolarity.Beneficial, true), (3046, BuffPolarity.Beneficial, true), (3047, BuffPolarity.Beneficial, true),
        (3048, BuffPolarity.Beneficial, true), (3049, BuffPolarity.Beneficial, true), (3050, BuffPolarity.Beneficial, true),
        (3051, BuffPolarity.Beneficial, true), (3052, BuffPolarity.Beneficial, true), (3053, BuffPolarity.Beneficial, true),
        (3056, BuffPolarity.Beneficial, true), (3057, BuffPolarity.Beneficial, true), (3059, BuffPolarity.Beneficial, true),
        (4001, BuffPolarity.Harmful, true), (4002, BuffPolarity.Harmful, true), (4003, BuffPolarity.Harmful, true),
        (4004, BuffPolarity.Harmful, true), (4006, BuffPolarity.Harmful, true), (4008, BuffPolarity.Harmful, true),
        (4009, BuffPolarity.Harmful, true), (4011, BuffPolarity.Harmful, true), (4012, BuffPolarity.Harmful, true),
        (4013, BuffPolarity.Harmful, true), (4014, BuffPolarity.Harmful, true), (4015, BuffPolarity.Harmful, true),
        (4016, BuffPolarity.Harmful, true), (4017, BuffPolarity.Harmful, true),
        (5001, BuffPolarity.Passive, true), (5002, BuffPolarity.Passive, true), (5003, BuffPolarity.Passive, false),
        (5004, BuffPolarity.Passive, false), (5005, BuffPolarity.Beneficial, false), (5006, BuffPolarity.Passive, true),
        (5007, BuffPolarity.Beneficial, true), (5008, BuffPolarity.Passive, true), (5009, BuffPolarity.Passive, true),
        (5010, BuffPolarity.Passive, true), (5011, BuffPolarity.Passive, false), (5012, BuffPolarity.Passive, true),
        (5013, BuffPolarity.Passive, false), (5014, BuffPolarity.Passive, false)
    };

    private static readonly int[] DeletedBuffIds = { 1011, 1012, 1016, 1023, 3007, 3017, 3022, 3038, 3039, 3040, 3055, 3058, 3060, 4005, 4007 };

    private static void VerifyStackRules()
    {
        foreach (var (id, polarity, stackable) in SpecBuffs)
        {
            Eq(BuffMetadataDatabase.TryGetBuffData(id, out _), true, $"버프 {id} 데이터 존재");
            Eq(BuffData.GetPolarity(id), polarity, $"버프 {id}의 극성");
            Eq(BuffData.IsNonStackableBuffId(id), !stackable, $"버프 {id}의 중첩 규칙");
        }

        foreach (int id in DeletedBuffIds)
        {
            Eq(BuffMetadataDatabase.TryGetBuffData(id, out _), false, $"삭제된 버프 {id} 데이터 없음");
        }

        Eq(WhirlpoolBuffId, 4017, "소용돌이 식별자 이동");
        Eq(BuffData.IsHarmfulBuffId(WhirlpoolBuffId), true, "소용돌이는 해로운 효과");
        Eq(DrowningBuffId, 4016, "익사 식별자 이전");
        Eq(BuffData.IsBeneficialBuffId(DrowningBuffId), false, "익사는 해로운 효과");
        Eq(BuffData.IsHarmfulBuffId(MirrorBuffId), false, "몬스터 패시브는 해로운 효과 아님");
        Eq(BuffData.IsRemovableBeneficialBuffId(StrengthBuffId), true, "힘은 제거 가능한 이로운 효과");
        Eq(BuffData.IsRemovableBeneficialBuffId(PoisonUpgradeBuffId), false, "5xxx 이로운 효과는 훔치기·제거 대상 아님");
        Eq(BuffData.AllowsNegativeStack(StrengthBuffId) && BuffData.AllowsNegativeStack(StrengthDecayBuffId), true, "힘과 근육 이완은 음수 허용");
        Eq(BuffData.AllowsNegativeStack(WeakBuffId), false, "약화는 음수 불가");

        var battle = new TrainingBattleManager();
        battle.playerData.AddBuff(ComboBuffId, 3);
        battle.playerData.AddBuff(ComboBuffId, 2);
        Eq(battle.playerData.GetBuffStack(ComboBuffId), 5, "콤보 획득량 누적");
        battle.playerData.SetBuffStack(ComboBuffId, 8);
        Eq(battle.playerData.GetBuffStack(ComboBuffId), 8, "콤보 직접 설정 중첩 유지");

        battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.AddBuff(StrengthBuffId, -6);
        Eq(player.GetBuffStack(StrengthBuffId), -6, "음수 힘 보유");
        Eq(player.CalculateCardDamage(10), 4, "음수 힘은 공격 피해 감소");
        Eq(player.CalculateCardDamage(3), 0, "공격 피해 하한 0");
        player.AddBuff(StrengthBuffId, 6);
        Eq(player.currentBuffs.Exists(buff => buff.data.buffId == StrengthBuffId), false, "힘이 0이 되면 제거");
        battle.ApplyBuffToPlayer(StrengthBuffId, -2);
        Eq(player.GetBuffStack(StrengthBuffId), -2, "버프 진입점의 음수 힘 적용");
        player.AddBuff(DrawLockBuffId, 1);
        battle.ApplyBuffToPlayer(DrawLockBuffId, -1);
        Eq(player.GetBuffStack(DrawLockBuffId), 0, "중첩 불가 효과에 음수 부여 시 제거");
        player.AddBuff(StrengthDecayBuffId, -3);
        battle.playerService.ReplayTurnEndTriggeredEffects();
        Eq(player.GetBuffStack(StrengthBuffId), 1, "음수 근육 이완은 턴 종료 시 힘 증가");
        battle.playerService.ReplayTurnEndTriggeredEffects();
        Eq(player.GetBuffStack(StrengthBuffId), 4, "근육 이완은 이중 동작 반복 대상");
        battle.playerService.OnPlayerTurnEnd();
        Eq(player.GetBuffStack(StrengthDecayBuffId), 0, "근육 이완은 턴 종료 처리 후 제거");
    }

    private static void VerifyRepeatCharges()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var monster = new Monster();
        battle.monsters.Add(monster);
        var card = new Card();
        card.effects.Add(new AttackEffect { amount = 5, target = TargetType.SingleEnemy });
        player.AddBuff(RepeatNextCardBuffId, 2);

        Eq(battle.playerService.ConsumeRepeatCount(card, true), 0, "반복 실행은 재사용을 소모하지 않음");
        Eq(battle.playerService.ConsumeRepeatCount(null, false), 0, "카드 없는 실행은 재사용을 소모하지 않음");
        Eq(player.GetBuffStack(RepeatNextCardBuffId), 2, "재사용 두 번 유지");
        Play(battle, card, monster);
        Eq(monster.hp, 90, "첫 카드 한 번 추가 사용");
        Eq(player.GetBuffStack(RepeatNextCardBuffId), 1, "첫 카드 이후 한 번 남음");
        Play(battle, card, monster);
        Eq(monster.hp, 80, "둘째 카드 한 번 추가 사용");
        Eq(player.GetBuffStack(RepeatNextCardBuffId), 0, "둘째 카드 이후 재사용 제거");
        Play(battle, card, monster);
        Eq(monster.hp, 75, "재사용 소진 후 추가 사용 없음");
    }

    private static void VerifyComboDamage()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        var monster = new Monster();
        battle.monsters.Add(monster);
        player.AddBuff(ComboBuffId, 1);
        new DamageEffect { amount = 1 }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 1, "공격이 아닌 피해는 콤보 미누적 (E-29)");
        for (int i = 0; i < 5; i++)
        {
            new AttackEffect { amount = 1, target = TargetType.SingleEnemy }.Execute(battle);
            Eq(player.GetBuffStack(ComboBuffId), i + 2, "공격 타격마다 콤보 1중첩 증가");
        }
        battle.Buffs.Player.OnPlayerDamageDealt(monster, 0);
        Eq(player.GetBuffStack(ComboBuffId), 6, "피해가 없으면 콤보 유지");
        // 이후 수치는 기존 검증과 같은 체력 95 기준
        monster.hp = 95;
        new AttackEffect { amount = 10, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(monster.hp, 85, "7중첩에 도달시키는 공격은 원래 피해");
        Eq(player.GetBuffStack(ComboBuffId), 7, "공격 피해로 7중첩 도달");
        Eq(player.CalculateCardDamage(10), 20, "준비된 콤보 공격 미리보기");
        Eq(player.CalculateCardDamage(10), 20, "반복 미리보기 결과 유지");
        Eq(player.CalculateCardDamage(10, 1f, 1f, false), 10, "준비된 콤보 일반 피해 미증폭");
        Eq(player.GetBuffStack(ComboBuffId), 7, "미리보기는 콤보 미소모");
        new DamageEffect { amount = 5 }.Execute(battle);
        Eq(monster.hp, 80, "7중첩 이후 일반 피해도 미증폭");
        Eq(player.GetBuffStack(ComboBuffId), 7, "일반 피해는 콤보 미누적");
        new AttackEffect { amount = 10, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(monster.hp, 60, "다음 공격 피해 두 배");
        Eq(player.GetBuffStack(ComboBuffId), 0, "초과 중첩도 발동 후 완전히 제거");
        Eq(player.currentBuffs.Exists(buff => buff.data.buffId == ComboBuffId), false, "콤보 항목 제거");
        new AttackEffect { amount = 1, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 0, "발동 후 자동 재누적 없음");
        player.AddBuff(ComboBuffId, 1);
        new AttackEffect { amount = 1, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 2, "재획득한 콤보는 새 중첩부터 시작");
    }

    private static void VerifyComboTargets()
    {
        var battle = new TrainingBattleManager();
        var player = battle.playerData;
        player.AddBuff(ComboBuffId, 7);
        new AttackEffect { amount = 10, target = TargetType.AllEnemies }.Execute(battle);
        new AttackEffect { amount = 10, target = TargetType.RandomEnemy }.Execute(battle);
        new AttackEffect { amount = 10, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 7, "대상 없는 공격은 콤보 유지");
        var first = new Monster();
        var second = new Monster();
        battle.monsters.Add(first);
        battle.monsters.Add(second);
        new AttackEffect { amount = 10, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 7, "단일 대상 미선택 시 콤보 유지");
        player.SetBuffStack(ComboBuffId, 6);
        new AttackEffect { amount = 10, target = TargetType.AllEnemies }.Execute(battle);
        Eq(first.hp, 90, "광역 첫 대상은 7중첩 도달 피해");
        Eq(second.hp, 80, "광역 다음 대상에 콤보 적용");
        Eq(player.GetBuffStack(ComboBuffId), 0, "광역 콤보 발동 후 제거");
        player.AddBuff(ComboBuffId, 7);
        new AttackEffect { amount = 5, target = TargetType.RandomEnemy }.Execute(battle);
        Eq(first.hp, 80, "무작위 공격 콤보 피해");
        Eq(player.GetBuffStack(ComboBuffId), 0, "무작위 공격 콤보 소모");
        player.AddBuff(ComboBuffId, 7);
        battle.currentTarget = new Monster { hp = 0 };
        new AttackEffect { amount = 5, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 7, "죽은 대상 공격은 콤보 유지");
    }

    private static void Play(TrainingBattleManager battle, Card card, Monster target)
    {
        var sequence = CardEffectSequence.Run(TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(
            battle, card, target, triggerPowerEffects: true, allowRepeats: true), battle);
        while (sequence.MoveNext()) { }
    }
}
