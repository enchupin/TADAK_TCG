using System;
using System.Collections.Generic;
using UnityEngine;

public static class BossModeSession
{
    public static bool IsActive { get; private set; }
    public static long TotalDamage { get; private set; }
    private static bool saved;
    private static readonly List<int> cardIds = new List<int>();

    public static void Begin(List<Character> characters, List<int> selectedCardIds)
    {
        Reset();
        TrainingRunState.ResetRun();
        PlayerData.Reset();
        TrainingBattleManager.buildingDeck = null;
        SelectedButtonControl.selectedCharacterList = new List<Character>(characters);
        cardIds.AddRange(selectedCardIds);
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
        TotalDamage = 0;
        saved = false;
        cardIds.Clear();
    }
}
