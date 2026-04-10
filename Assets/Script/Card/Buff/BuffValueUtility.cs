using static BattleRuntimeDefinitions;

public static class BuffValueUtility
{
    public const float CorrosionIncomingDamageMultiplier = 1.25f;
    public const float EnhancedCorrosionIncomingDamageMultiplier = 1.5f;
    public const float WeakOutgoingDamageMultiplier = 0.75f;

    public static float GetIncomingDamageMultiplier(int buffId)
    {
        if (buffId == EnhancedCorrosionBuffId)
        {
            return EnhancedCorrosionIncomingDamageMultiplier;
        }

        if (buffId == CorrosionBuffId)
        {
            return CorrosionIncomingDamageMultiplier;
        }

        return 1f;
    }

    public static float GetOutgoingDamageMultiplier(int buffId)
    {
        if (buffId == WeakBuffId)
        {
            return WeakOutgoingDamageMultiplier;
        }

        return 1f;
    }
}
