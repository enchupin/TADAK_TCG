using System;
using System.Collections.Generic;
using UnityEngine;

public static class BossModeSession
{
    public static bool IsActive { get; private set; }
    public static int SelectedBossId { get; private set; }
    public static long TotalDamage { get; private set; }
    private static bool saved;
    private static readonly List<int> cardIds = new List<int>();

    // 로비 보스 선택 ID(원본 301 ~ 304 또는 v1.0 5301 ~ 5304) → 보스 조우 BS01 ~ BS04 (8-6)
    public static string GetBossEncounterId(int bossId)
    {
        return (bossId % 1000) switch
        {
            301 => "BS01",
            302 => "BS02",
            303 => "BS03",
            304 => "BS04",
            _ => null
        };
    }

    public static bool IsValidBoss(int bossId)
    {
        return GetBossEncounterId(bossId) != null;
    }

    public static void Begin(List<Character> characters, List<int> selectedCardIds, int bossId)
    {
        if (!IsValidBoss(bossId)) throw new ArgumentOutOfRangeException(nameof(bossId));
        Reset();
        TrainingRunState.ResetRun();
        PlayerData.Reset();
        TrainingBattleManager.buildingDeck = null;
        SelectedButtonControl.selectedCharacterList = new List<Character>(characters);
        // 훈련 모드와 같이 ID 순서로 배치
        SelectedButtonControl.selectedCharacterList.Sort();
        cardIds.AddRange(selectedCardIds);
        TrainingRunDeckPersistence.ResetPermanentDamageBonuses();
        SelectedBossId = bossId;
        IsActive = true;
    }

    public static BuildingDeck CreateDeck()
    {
        BuildingDeck deck = new BuildingDeck();
        deck.InitializeFromCardIds(cardIds);
        return deck;
    }

    public static void AddDamage(int amount)
    {
        if (IsActive && !saved && amount > 0) {
            TotalDamage += Math.Min((long)amount, long.MaxValue - TotalDamage);
        }
    }

    public static void SaveResult()
    {
        if (!IsActive || saved) return;
        try {
            PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
            profile.bossModeLastDamage = TotalDamage;
            profile.bossModeBestDamage = Math.Max(profile.bossModeBestDamage, TotalDamage);
            profile.bossModeLastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            ProfileSaveManager.Save(profile);
            saved = true;
        }
        catch (Exception exception) {
            Debug.LogError($"[BossModeSession] 피해 기록을 저장하지 못했습니다: {exception.Message}");
        }
    }

    public static void Reset()
    {
        IsActive = false;
        SelectedBossId = 0;
        TotalDamage = 0;
        saved = false;
        cardIds.Clear();
    }
}
