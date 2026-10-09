using System;
using static BattleRuntimeDefinitions;

static class BuffApplicationTests
{
    private static int assertions;

    public static int Run()
    {
        assertions = 0;
        var battle = new TrainingBattleManager();
        battle.ApplyBuffToPlayer(RepeatNextCardBuffId, 2);
        battle.Buffs.ApplyToPlayer(RepeatNextCardBuffId, 3);
        Eq(battle.playerData.GetBuffStack(RepeatNextCardBuffId), 5, "기존 진입점과 버프 진입점은 같은 상태에 누적");
        battle.ApplyBuffToPlayer(RepeatNextCardBuffId, 0);
        battle.ApplyBuffToPlayer(RepeatNextCardBuffId, -2);
        Eq(battle.playerData.GetBuffStack(RepeatNextCardBuffId), 5, "0 이하의 적용량은 무시");

        battle.playerData.AddBuff(OverchargeBuffId, 2);
        battle.ApplyBuffToPlayer(RuneBuffId, 3);
        Eq(battle.playerData.GetBuffStack(RuneBuffId), 5, "룬 특별 적용은 보정 후 한 번 저장");
        battle.ApplyBuffToPlayer(RuneBuffId, 0);
        Eq(battle.playerData.GetBuffStack(RuneBuffId), 5, "0 적용으로 특별 훅이 실행되지 않음");

        var first = new Monster();
        var second = new Monster();
        var dead = new Monster { hp = 0 };
        battle.monsters.AddRange(new[] { first, second, dead });
        battle.ApplyBuffToAllEnemies(StrengthBuffId, 2);
        Eq(first.GetBuffStack(StrengthBuffId), 2, "일반 광역 버프 첫 대상");
        Eq(second.GetBuffStack(StrengthBuffId), 2, "일반 광역 버프 둘째 대상");
        Eq(dead.GetBuffStack(StrengthBuffId), 0, "죽은 적은 광역 부여 제외");
        Eq(battle.battleContext.EnemyDebuffNotifications, 0, "이로운 효과는 디버프 통계 제외");
        battle.ApplyBuffToMonster(first, DrowningBuffId, 2);
        Eq(first.GetBuffStack(DrowningBuffId), 2, "일반 해로운 효과 저장");
        Eq(battle.battleContext.EnemyDebuffNotifications, 1, "일반 해로운 효과 알림 한 번");
        battle.ApplyBuffToMonster(dead, DrowningBuffId, 2);
        battle.ApplyBuffToMonster(null, DrowningBuffId, 2);
        battle.ApplyBuffToMonster(first, DrowningBuffId, 0);
        Eq(battle.battleContext.EnemyDebuffNotifications, 1, "무효 대상과 적용량은 알림 제외");

        battle = new TrainingBattleManager();
        var potion = new Card { cardId = 101080 };
        battle.usableDeckManager.draw.Add(potion);
        battle.ApplyBuffToPlayer(PotionEnhanceBuffId, 1);
        Eq(potion.cardId, 101081, "버프 적용 완료 후 덱의 지속 강화");
        var newPotion = new Card { cardId = 101082 };
        Eq(ReferenceEquals(battle.ApplyPersistentUpgradeToCard(newPotion, -1), newPotion), true, "지속 강화는 원래 카드 객체를 유지");
        Eq(newPotion.cardId, 101083, "버프 출처가 지정되지 않은 강화 경로 유지");
        return assertions;
    }

    private static void Eq<T>(T actual, T expected, string message)
    {
        assertions++;
        if (!Equals(actual, expected)) throw new Exception($"{message}: 기대 {expected}, 실제 {actual}");
    }
}
