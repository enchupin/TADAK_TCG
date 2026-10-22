using System.Collections.Generic;
using static BattleRuntimeDefinitions;

// 1024 로열: 한 턴에 파티 3명의 카드를 모두 쓸 때마다 에너지 1
public sealed class RoyalBuffScript : PlayerBuffScript
{
    private const int EnergyAmount = 1;

    private readonly HashSet<Character> playedCharacters = new();

    public override int BuffId => RoyalBuffId;

    public int PlayedCharacterCount => playedCharacters.Count;
    // 툴팁 진행도: 이번 바퀴에 카드를 쓴 캐릭터
    public bool HasPlayedCharacter(Character character) => playedCharacters.Contains(character);
    // 툴팁 진행도의 목표 인원
    public int GetRequiredCharacterCount(TrainingBattleManager battleManager) => battleManager != null ? GetPartySize(battleManager) : GameRules.Deck.partySize;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        playedCharacters.Clear();
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        playedCharacters.Clear();
    }

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || playedCard == null || isRepeatedEffect || stack <= 0
            || playedCard.character == Character.Monster)
        {
            return;
        }

        playedCharacters.Add(playedCard.character);
        if (playedCharacters.Count < GetPartySize(battleManager))
        {
            return;
        }

        // 세 명을 모두 채울 때마다 발동하고 다시 처음부터 셈
        playedCharacters.Clear();
        player.AddEnergy(EnergyAmount);
    }

    private static int GetPartySize(TrainingBattleManager battleManager)
    {
        int partySize = battleManager.GetIdentityStates().Count;
        return partySize > 0 ? partySize : GameRules.Deck.partySize;
    }
}
