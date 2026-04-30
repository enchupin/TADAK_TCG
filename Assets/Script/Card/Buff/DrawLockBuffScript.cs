using static BattleRuntimeDefinitions;

public sealed class DrawLockBuffScript : PlayerBuffScript
{
    public override int BuffId => DrawLockBuffId;

    public override bool CanDrawCards(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentCanDraw)
    {
        return stack > 0 ? false : currentCanDraw;
    }

    public override bool CanGainCardsToHand(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentCanGain)
    {
        return stack > 0 ? false : currentCanGain;
    }

    public override bool CanGainCardsToHandFrom(TrainingBattleManager battleManager, PlayerData player, MoveZoneType from, string subject, int stack, bool currentCanGain)
    {
        if (!currentCanGain || stack <= 0)
        {
            return currentCanGain;
        }

        switch (from)
        {
            case MoveZoneType.DrawPile:
            case MoveZoneType.DiscardPile:
                return false;
            case MoveZoneType.Source:
                return !IsSelectedHandGainSubject(subject);
            default:
                return true;
        }
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.RemoveBuffStack(BuffId);
    }

    private static bool IsSelectedHandGainSubject(string subject)
    {
        return string.Equals(subject, "Selected", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(subject, "SelectedCard", System.StringComparison.OrdinalIgnoreCase);
    }
}
