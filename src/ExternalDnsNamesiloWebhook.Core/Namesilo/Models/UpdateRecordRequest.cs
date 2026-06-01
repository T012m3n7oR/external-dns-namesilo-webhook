using System.Collections.Generic;
using System.Globalization;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class UpdateRecordRequest : INamesiloApiRequest
{
    required public string Domain { get; init; }

    required public string RecordId { get; init; }

    required public string RecordHost { get; init; }

    required public string RecordValue { get; init; }

    required public int Ttl { get; init; }

    public IReadOnlyDictionary<string, string> ToQueryParameters()
    {
        return new Dictionary<string, string>
        {
            ["domain"] = Domain,
            ["rrid"] = RecordId,
            ["rrhost"] = RecordHost,
            ["rrvalue"] = RecordValue,
            ["rrttl"] = Ttl.ToString(CultureInfo.InvariantCulture),
        };
    }
}
