using System.Collections.Generic;
using UnityEngine;

public static class BattleRuntimeDefinitions
{
    public const int TemporaryLavaSkinBuffId = 930020;

    private static readonly Dictionary<string, int> BuffIds = new Dictionary<string, int>();
    private static bool isInitialized;

    public static int CorrosionEnhanceBuffId => GetBuffId(nameof(CorrosionEnhanceBuffId));
    public static int PotionEnhanceBuffId => GetBuffId(nameof(PotionEnhanceBuffId));
    public static int PotionCycleBuffId => GetBuffId(nameof(PotionCycleBuffId));
    public static int GlacierShapeEnhanceBuffId => GetBuffId(nameof(GlacierShapeEnhanceBuffId));
    public static int UnplayableUnlockBuffId => GetBuffId(nameof(UnplayableUnlockBuffId));
    public static int DoubleJackBuffId => GetBuffId(nameof(DoubleJackBuffId));
    public static int DuplicateGenerateBuffId => GetBuffId(nameof(DuplicateGenerateBuffId));
    public static int HighCostRepeatBuffId => GetBuffId(nameof(HighCostRepeatBuffId));
    public static int StraightBuffId => GetBuffId(nameof(StraightBuffId));
    public static int RuneBarrierBuffId => GetBuffId(nameof(RuneBarrierBuffId));
    public static int EfficientBarrierBuffId => GetBuffId(nameof(EfficientBarrierBuffId));
    public static int PermanentBarrierRetentionBuffId => GetBuffId(nameof(PermanentBarrierRetentionBuffId));
    public static int WuppiGuardSwitchBuffId => GetBuffId(nameof(WuppiGuardSwitchBuffId));
    public static int FeatherCycleBuffId => GetBuffId(nameof(FeatherCycleBuffId));
    public static int FeatherEnhanceBuffId => GetBuffId(nameof(FeatherEnhanceBuffId));
    public static int SoulProtectionBuffId => GetBuffId(nameof(SoulProtectionBuffId));
    public static int WuppiAttackSwitchBuffId => GetBuffId(nameof(WuppiAttackSwitchBuffId));
    public static int IntentionalRageBuffId => GetBuffId(nameof(IntentionalRageBuffId));
    public static int BloodBattleBuffId => GetBuffId(nameof(BloodBattleBuffId));
    public static int LastStandBuffId => GetBuffId(nameof(LastStandBuffId));
    public static int DoubleFeatherBuffId => GetBuffId(nameof(DoubleFeatherBuffId));
    public static int DrawLockBuffId => GetBuffId(nameof(DrawLockBuffId));
    public static int FlameTransferBuffId => GetBuffId(nameof(FlameTransferBuffId));
    public static int RegenerationBuffId => GetBuffId(nameof(RegenerationBuffId));
    public static int StrengthBuffId => GetBuffId(nameof(StrengthBuffId));
    public static int DelayedPotionFactoryBuffId => GetBuffId(nameof(DelayedPotionFactoryBuffId));
    public static int CorrosionOnPotionUseBuffId => GetBuffId(nameof(CorrosionOnPotionUseBuffId));
    public static int BioExperimentAllBuffId => GetBuffId(nameof(BioExperimentAllBuffId));
    public static int PotionFactoryBuffId => GetBuffId(nameof(PotionFactoryBuffId));
    public static int ExtraDrawBuffId => GetBuffId(nameof(ExtraDrawBuffId));
    public static int AttackBoostBuffId => GetBuffId(nameof(AttackBoostBuffId));
    public static int ThornBuffId => GetBuffId(nameof(ThornBuffId));
    public static int BarrierRetentionBuffId => GetBuffId(nameof(BarrierRetentionBuffId));
    public static int GlacierBondBuffId => GetBuffId(nameof(GlacierBondBuffId));
    public static int AbsoluteZeroBuffId => GetBuffId(nameof(AbsoluteZeroBuffId));
    public static int GlacierShapeOnHitBuffId => GetBuffId(nameof(GlacierShapeOnHitBuffId));
    public static int ColdAirBuffId => GetBuffId(nameof(ColdAirBuffId));
    public static int EvadeBuffId => GetBuffId(nameof(EvadeBuffId));
    public static int OverheatBuffId => GetBuffId(nameof(OverheatBuffId));
    public static int OverheatGrowthBuffId => GetBuffId(nameof(OverheatGrowthBuffId));
    public static int FlameConductionBuffId => GetBuffId(nameof(FlameConductionBuffId));
    public static int LavaSkinBuffId => GetBuffId(nameof(LavaSkinBuffId));
    public static int IntimidationBuffId => GetBuffId(nameof(IntimidationBuffId));
    public static int RepeatNextCardBuffId => GetBuffId(nameof(RepeatNextCardBuffId));
    public static int CardUseAllEnemiesDamageBuffId => GetBuffId(nameof(CardUseAllEnemiesDamageBuffId));
    public static int DamageClampToOneBuffId => GetBuffId(nameof(DamageClampToOneBuffId));
    public static int NextCardFreeBuffId => GetBuffId(nameof(NextCardFreeBuffId));
    public static int RetainChoiceBuffId => GetBuffId(nameof(RetainChoiceBuffId));
    public static int JokerPowerBuffId => GetBuffId(nameof(JokerPowerBuffId));
    public static int RuneBuffId => GetBuffId(nameof(RuneBuffId));
    public static int RuneGenerationBuffId => GetBuffId(nameof(RuneGenerationBuffId));
    public static int LavaBarrierBuffId => GetBuffId(nameof(LavaBarrierBuffId));
    public static int FeatherStackBoostBuffId => GetBuffId(nameof(FeatherStackBoostBuffId));
    public static int RepeatNextPowerCardBuffId => GetBuffId(nameof(RepeatNextPowerCardBuffId));
    public static int GrowingFeatherBuffId => GetBuffId(nameof(GrowingFeatherBuffId));
    public static int PrecisionBuffId => GetBuffId(nameof(PrecisionBuffId));
    public static int FeatherAutoTriggerBuffId => GetBuffId(nameof(FeatherAutoTriggerBuffId));
    public static int BlindFeatherBuffId => GetBuffId(nameof(BlindFeatherBuffId));
    public static int DeadlyAmbushBuffId => GetBuffId(nameof(DeadlyAmbushBuffId));
    public static int ExhaustDrawContractBuffId => GetBuffId(nameof(ExhaustDrawContractBuffId));
    public static int StrengthContractBuffId => GetBuffId(nameof(StrengthContractBuffId));
    public static int WuppiGuardBuffId => GetBuffId(nameof(WuppiGuardBuffId));
    public static int WuppiAttackBuffId => GetBuffId(nameof(WuppiAttackBuffId));
    public static int DoubleActionBuffId => GetBuffId(nameof(DoubleActionBuffId));
    public static int ModeCycleBuffId => GetBuffId(nameof(ModeCycleBuffId));
    public static int OverchargeBuffId => GetBuffId(nameof(OverchargeBuffId));
    public static int ComboBuffId => GetBuffId(nameof(ComboBuffId));
    public static int BurningWillBuffId => GetBuffId(nameof(BurningWillBuffId));
    public static int IndomitableBuffId => GetBuffId(nameof(IndomitableBuffId));
    public static int VictorRestBuffId => GetBuffId(nameof(VictorRestBuffId));
    public static int BerserkerBuffId => GetBuffId(nameof(BerserkerBuffId));
    public static int EnergyOverflowBuffId => GetBuffId(nameof(EnergyOverflowBuffId));
    public static int DrowningBuffId => GetBuffId(nameof(DrowningBuffId));
    public static int WhirlpoolBuffId => GetBuffId(nameof(WhirlpoolBuffId));
    public static int UnderwaterBreathingBuffId => GetBuffId(nameof(UnderwaterBreathingBuffId));
    public static int EncroachmentBuffId => GetBuffId(nameof(EncroachmentBuffId));
    public static int RisingWaterBuffId => GetBuffId(nameof(RisingWaterBuffId));
    public static int DrowningOnDebuffBuffId => GetBuffId(nameof(DrowningOnDebuffBuffId));
    public static int TranceBuffId => GetBuffId(nameof(TranceBuffId));
    public static int CorrosionBuffId => GetBuffId(nameof(CorrosionBuffId));
    public static int EnhancedCorrosionBuffId => GetBuffId(nameof(EnhancedCorrosionBuffId));
    public static int BurnBuffId => GetBuffId(nameof(BurnBuffId));
    public static int FreezeBuffId => GetBuffId(nameof(FreezeBuffId));
    public static int ThornDecayBuffId => GetBuffId(nameof(ThornDecayBuffId));
    public static int StrengthDecayBuffId => GetBuffId(nameof(StrengthDecayBuffId));
    public static int OverheatDecayBuffId => GetBuffId(nameof(OverheatDecayBuffId));
    public static int WeakBuffId => GetBuffId(nameof(WeakBuffId));
    public static int CrueltyDebuffId => GetBuffId(nameof(CrueltyDebuffId));
    public static int FrailBuffId => GetBuffId(nameof(FrailBuffId));
    public static int DrawInterferenceBuffId => GetBuffId(nameof(DrawInterferenceBuffId));
    public static int LifeLinkBuffId => GetBuffId(nameof(LifeLinkBuffId));
    public static int FeatherBuffId => GetBuffId(nameof(FeatherBuffId));
    public static int MonsterLifeStealBuffId => GetBuffId(nameof(MonsterLifeStealBuffId));
    public static int VoidShellBuffId => GetBuffId(nameof(VoidShellBuffId));
    public static int FaithfulPrayerBuffId => GetBuffId(nameof(FaithfulPrayerBuffId));
    public static int ParasiticMushroomBuffId => GetBuffId(nameof(ParasiticMushroomBuffId));
    public static int PoisonUpgradeBuffId => GetBuffId(nameof(PoisonUpgradeBuffId));
    public static int PranksterGhostBuffId => GetBuffId(nameof(PranksterGhostBuffId));
    public static int ThiefBuffId => GetBuffId(nameof(ThiefBuffId));
    public static int BurningFlameBuffId => GetBuffId(nameof(BurningFlameBuffId));
    public static int EightLegsBuffId => GetBuffId(nameof(EightLegsBuffId));
    public static int FuturePredationBuffId => GetBuffId(nameof(FuturePredationBuffId));
    public static int RootedBuffId => GetBuffId(nameof(RootedBuffId));
    public static int MirrorBuffId => GetBuffId(nameof(MirrorBuffId));
    public static int PoisonousMushroomBuffId => GetBuffId(nameof(PoisonousMushroomBuffId));

    public static void Initialize()
    {
        EnsureInitialized();
    }

    private static int GetBuffId(string key)
    {
        EnsureInitialized();
        return BuffIds[key];
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        BuffIds.Clear();
        BuffMetadataDatabase.Preload();

        Register(nameof(CorrosionEnhanceBuffId), "강화: 부식", 1001);
        Register(nameof(PotionEnhanceBuffId), "강화: 포션", 1002);
        Register(nameof(PotionCycleBuffId), "순환: 포션", 1003);
        Register(nameof(GlacierShapeEnhanceBuffId), "강화: 냉기", 1004);
        Register(nameof(UnplayableUnlockBuffId), "금기 해제", 1006);
        Register(nameof(DoubleJackBuffId), "더블", 1007);
        Register(nameof(DuplicateGenerateBuffId), "복제 생성", 1008);
        Register(nameof(HighCostRepeatBuffId), "풀하우스", 1009);
        Register(nameof(StraightBuffId), "스트레이트", 1010);
        Register(nameof(RuneBarrierBuffId), "보호 룬", 1011);
        Register(nameof(EfficientBarrierBuffId), "효율", 1012);
        Register(nameof(PermanentBarrierRetentionBuffId), "영구 동결", 1013);
        Register(nameof(WuppiGuardSwitchBuffId), "전환: 보호", 1014);
        Register(nameof(FeatherCycleBuffId), "순환: 깃털", 1015);
        Register(nameof(FeatherEnhanceBuffId), "강화: 깃털", 1016);
        Register(nameof(SoulProtectionBuffId), "영혼 보호", 1017);
        Register(nameof(WuppiAttackSwitchBuffId), "전환: 공격", 1018);
        Register(nameof(IntentionalRageBuffId), "의도된 분노", 1019);
        Register(nameof(BloodBattleBuffId), "혈투", 1020);
        Register(nameof(LastStandBuffId), "구사일생", 1021);
        Register(nameof(DoubleFeatherBuffId), "이중 깃털", 1022);
        Register(nameof(DrawLockBuffId), "드로우 불가", 2001);
        Register(nameof(FlameTransferBuffId), "화염 전이", 2002);
        Register(nameof(RegenerationBuffId), "재생", 3001);
        Register(nameof(StrengthBuffId), "힘", 3002);
        Register(nameof(DelayedPotionFactoryBuffId), "제조", 3003);
        Register(nameof(CorrosionOnPotionUseBuffId), "폐기물", 3004);
        Register(nameof(BioExperimentAllBuffId), "잔악무도", 3006);
        Register(nameof(PotionFactoryBuffId), "제조 시설", 3007);
        Register(nameof(ExtraDrawBuffId), "강화: 드로우", 3008);
        Register(nameof(AttackBoostBuffId), "강공", 3009);
        Register(nameof(ThornBuffId), "가시", 3010);
        Register(nameof(BarrierRetentionBuffId), "예방", 3011);
        Register(nameof(GlacierBondBuffId), "결속", 3012);
        Register(nameof(AbsoluteZeroBuffId), "절대영도", 3013);
        Register(nameof(GlacierShapeOnHitBuffId), "산산조각", 3014);
        Register(nameof(ColdAirBuffId), "냉기", 3015);
        Register(nameof(EvadeBuffId), "회피", 3016);
        Register(nameof(OverheatBuffId), "과열", 3017);
        Register(nameof(OverheatGrowthBuffId), "발열", 3018);
        Register(nameof(FlameConductionBuffId), "불주먹", 3019);
        Register(nameof(LavaSkinBuffId), "용암 피부", 3020);
        Register(nameof(IntimidationBuffId), "위압", 3022);
        Register(nameof(RepeatNextCardBuffId), "재사용", 3023);
        Register(nameof(CardUseAllEnemiesDamageBuffId), "연격", 3024);
        Register(nameof(DamageClampToOneBuffId), "아슬아슬", 3026);
        Register(nameof(NextCardFreeBuffId), "예고", 3027);
        Register(nameof(RetainChoiceBuffId), "준비", 3028);
        Register(nameof(JokerPowerBuffId), "광대", 3029);
        Register(nameof(RuneBuffId), "동력", 3031);
        Register(nameof(RuneGenerationBuffId), "배터리", 3032);
        Register(nameof(LavaBarrierBuffId), "용암 보호막", 3033);
        Register(nameof(FeatherStackBoostBuffId), "깃털 겹치기", 3034);
        Register(nameof(RepeatNextPowerCardBuffId), "파워 반복", 3035);
        Register(nameof(GrowingFeatherBuffId), "자라나는 깃털", 3036);
        Register(nameof(PrecisionBuffId), "정밀", 3037);
        Register(nameof(FeatherAutoTriggerBuffId), "자동 회수", 3038);
        Register(nameof(BlindFeatherBuffId), "눈 먼 깃털", 3039);
        Register(nameof(DeadlyAmbushBuffId), "치명적인 기습", 3040);
        Register(nameof(ExhaustDrawContractBuffId), "계약: 카드", 3041);
        Register(nameof(StrengthContractBuffId), "계약: 힘", 3042);
        Register(nameof(WuppiGuardBuffId), "우피: 보호", 3043);
        Register(nameof(WuppiAttackBuffId), "우피: 공격", 3044);
        Register(nameof(DoubleActionBuffId), "이중 동작", 3045);
        Register(nameof(ModeCycleBuffId), "순환: 모드", 3046);
        Register(nameof(OverchargeBuffId), "과충전", 3047);
        Register(nameof(ComboBuffId), "콤보", 3048);
        Register(nameof(BurningWillBuffId), "불타는 투지", 3049);
        Register(nameof(IndomitableBuffId), "불굴", 3050);
        Register(nameof(VictorRestBuffId), "승자의 휴식", 3051);
        Register(nameof(BerserkerBuffId), "광전사", 3052);
        Register(nameof(EnergyOverflowBuffId), "기력 초과", 3053);
        Register(nameof(DrowningBuffId), "익사", 3054);
        Register(nameof(WhirlpoolBuffId), "소용돌이", 3055);
        Register(nameof(UnderwaterBreathingBuffId), "수중 호흡", 3056);
        Register(nameof(EncroachmentBuffId), "잠식", 3057);
        Register(nameof(RisingWaterBuffId), "수면 상승", 3058);
        Register(nameof(DrowningOnDebuffBuffId), "부정한 물", 3059);
        Register(nameof(TranceBuffId), "무아지경", 3060);
        Register(nameof(CorrosionBuffId), "부식", 4001);
        Register(nameof(EnhancedCorrosionBuffId), "맹독", 4002);
        Register(nameof(BurnBuffId), "화염 낙인", 4003);
        Register(nameof(FreezeBuffId), "빙결", 4004);
        Register(nameof(ThornDecayBuffId), "재정비", 4005);
        Register(nameof(StrengthDecayBuffId), "근육 이완", 4006);
        Register(nameof(OverheatDecayBuffId), "화력 소진", 4007);
        Register(nameof(WeakBuffId), "약화", 4008);
        Register(nameof(CrueltyDebuffId), "잔혹", 4009);
        Register(nameof(FrailBuffId), "빈약", 4011);
        Register(nameof(DrawInterferenceBuffId), "드로우 방해", 4012);
        Register(nameof(LifeLinkBuffId), "생명 연결", 4013);
        Register(nameof(FeatherBuffId), "깃털", 4014);
        Register(nameof(MonsterLifeStealBuffId), "체력 강탈", 5001);
        Register(nameof(VoidShellBuffId), "공허 껍질", 5002);
        Register(nameof(FaithfulPrayerBuffId), "신실한 기도", 5003);
        Register(nameof(ParasiticMushroomBuffId), "기생하는 버섯", 5004);
        Register(nameof(PoisonUpgradeBuffId), "강화: 중독", 5005);
        Register(nameof(PranksterGhostBuffId), "장난꾸러기 유령", 5006);
        Register(nameof(ThiefBuffId), "도둑", 5007);
        Register(nameof(BurningFlameBuffId), "타오르는 불꽃", 5008);
        Register(nameof(EightLegsBuffId), "여덟 다리", 5009);
        Register(nameof(FuturePredationBuffId), "미래 포식", 5010);
        Register(nameof(RootedBuffId), "뿌리내림", 5011);
        Register(nameof(MirrorBuffId), "거울", 5012);
        Register(nameof(PoisonousMushroomBuffId), "독버섯", 5013);
    }

    private static void Register(string key, string buffName, int fallbackId)
    {
        if (BuffMetadataDatabase.TryGetBuffId(buffName, out int buffId))
        {
            BuffIds[key] = buffId;
            return;
        }

        BuffIds[key] = fallbackId;
        Debug.LogWarning($"[BattleRuntimeDefinitions] 버프 이름을 찾지 못해 fallback ID를 사용합니다: {buffName} -> {fallbackId}");
    }
}
