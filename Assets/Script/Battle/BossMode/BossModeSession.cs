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

    public static Type GetBossType(int monsterId)
    {
        return monsterId switch
        {
            301 => typeof(GiantFlowerSpiderBossMonster),
            302 => typeof(ProphetBossMonster),
            303 => typeof(IceAndFireBossMonster),
            304 => typeof(VoidLordBossMonster),
            _ => null
        };
    }

    public static void Begin(List<Character> characters, List<int> selectedCardIds, int bossId)
    {
        if (GetBossType(bossId) == null) throw new ArgumentOutOfRangeException(nameof(bossId));
        Reset();
        TrainingRunState.ResetRun();
        PlayerData.Reset();
        TrainingBattleManager.buildingDeck = null;
        SelectedButtonControl.selectedCharacterList = new List<Character>(characters);
        cardIds.AddRange(selectedCardIds);
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
