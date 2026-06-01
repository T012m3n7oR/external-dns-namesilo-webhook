namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

/// <summary>NameSilo <c>rrttl</c> constraints and normalization for DNS writes.</summary>
public static class NamesiloRecordTtl
{
    /// <summary>Minimum TTL accepted by NameSilo (API error 280 if lower).</summary>
    public const int MinimumSeconds = 3600;

    /// <summary>Maximum TTL accepted by NameSilo.</summary>
    public const int MaximumSeconds = 2592000;

    public const int DefaultSeconds = MinimumSeconds;

    public static int Normalize(int ttl)
    {
        if (ttl < MinimumSeconds)
        {
            return MinimumSeconds;
        }

        if (ttl > MaximumSeconds)
        {
            return MaximumSeconds;
        }

        return ttl;
    }
}
