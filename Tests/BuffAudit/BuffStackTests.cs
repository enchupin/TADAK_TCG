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

    private static void VerifyStackRules()
    {
        for (int id = 1000; id < 5000; id++)
        {
            Eq(BuffData.IsNonStackableBuffId(id), id < 3000, $"버프 {id}의 중첩 규칙");
            Eq(BuffData.IsBeneficialBuffId(id), (id / 1000) % 2 == 1, $"버프 {id}의 이로운 효과 규칙");
        }

        int[] monsterOnlyNonStackable = { FaithfulPrayerBuffId, ParasiticMushroomBuffId,
            PoisonUpgradeBuffId, RootedBuffId, HarmonyBuffId, PoisonousMushroomBuffId };
        foreach (int id in monsterOnlyNonStackable)
        {
            Eq(BuffData.IsNonStackableBuffId(id), true, $"몬스터 전용 버프 {id} 유지");
        }
        Eq(BuffData.IsNonStackableBuffId(MirrorBuffId), false, "몬스터 거울 중첩 유지");
        Eq(DrowningBuffId, 4016, "익사 식별자 이전");
        Eq(BuffData.IsBeneficialBuffId(DrowningBuffId), false, "익사는 해로운 효과");

        var battle = new TrainingBattleManager();
        battle.playerData.AddBuff(ComboBuffId, 3);
        battle.playerData.AddBuff(ComboBuffId, 2);
        Eq(battle.playerData.GetBuffStack(ComboBuffId), 5, "콤보 획득량 누적");
        battle.playerData.SetBuffStack(ComboBuffId, 8);
        Eq(battle.playerData.GetBuffStack(ComboBuffId), 8, "콤보 직접 설정 중첩 유지");
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
        for (int i = 0; i < 5; i++)
        {
            new DamageEffect { amount = 1 }.Execute(battle);
            Eq(player.GetBuffStack(ComboBuffId), i + 2, "일반 피해마다 콤보 1중첩 증가");
        }
        battle.HandlePlayerDamageDealt(monster, 0);
        Eq(player.GetBuffStack(ComboBuffId), 6, "피해가 없으면 콤보 유지");
        new AttackEffect { amount = 10, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(monster.hp, 85, "7중첩에 도달시키는 공격은 원래 피해");
        Eq(player.GetBuffStack(ComboBuffId), 7, "공격 피해로 7중첩 도달");
        Eq(player.CalculateCardDamage(10), 20, "준비된 콤보 공격 미리보기");
        Eq(player.CalculateCardDamage(10), 20, "반복 미리보기 결과 유지");
        Eq(player.CalculateCardDamage(10, 1f, 1f, false), 10, "준비된 콤보 일반 피해 미증폭");
        Eq(player.GetBuffStack(ComboBuffId), 7, "미리보기는 콤보 미소모");
        new DamageEffect { amount = 5 }.Execute(battle);
        Eq(monster.hp, 80, "7중첩 이후 일반 피해도 미증폭");
        Eq(player.GetBuffStack(ComboBuffId), 8, "일반 피해 후 콤보 유지와 누적");
        new AttackEffect { amount = 10, target = TargetType.SingleEnemy }.Execute(battle);
        Eq(monster.hp, 60, "다음 공격 피해 두 배");
        Eq(player.GetBuffStack(ComboBuffId), 0, "초과 중첩도 발동 후 완전히 제거");
        Eq(player.currentBuffs.Exists(buff => buff.data.buffId == ComboBuffId), false, "콤보 항목 제거");
        new DamageEffect { amount = 1 }.Execute(battle);
        Eq(player.GetBuffStack(ComboBuffId), 0, "발동 후 자동 재누적 없음");
        player.AddBuff(ComboBuffId, 1);
        new DamageEffect { amount = 1 }.Execute(battle);
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
