public class TriggerEffect : ICardEffect
{
    public string timing;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 1);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        if (battleManager == null || string.IsNullOrWhiteSpace(timing))
        {
            return;
        }

        int repeatCount = amount > 0 ? amount : 1;
        if (string.Equals(timing, "OnTurnEnd", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.AddTurnEndTriggerRepeat(repeatCount);
            return;
        }

        UnityEngine.Debug.LogWarning($"[TriggerEffect] 아직 지원하지 않는 timing입니다: {timing}");
    }
}
