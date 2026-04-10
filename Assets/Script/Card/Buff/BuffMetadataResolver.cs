public static class BuffMetadataResolver
{
    public static BuffData Resolve(int buffId)
    {
        if (BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData data)
            && data != null)
        {
            return data;
        }

        return new BuffData
        {
            buffId = buffId,
            name = $"버프 {buffId}",
            description = string.Empty
        };
    }
}
