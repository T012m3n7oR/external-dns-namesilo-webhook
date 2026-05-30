using System.Collections.Generic;
using System.Globalization;
using ExternalDnsNamesiloWebhook.Core.Enums;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class AddRecordRequest : INamesiloApiRequest
{
    required public string Domain { get; init; }

    required public DnsRecordType RecordType { get; init; }

    required public string RecordHost { get; init; }

    required public string RecordValue { get; init; }

    required public int Ttl { get; init; }

    public IReadOnlyDictionary<string, string> ToQueryParameters()
    {
        return new Dictionary<string, string>
        {
            ["domain"] = Domain,
            ["rrtype"] = RecordType.ToString(),
            ["rrhost"] = RecordHost,
            ["rrvalue"] = RecordValue,
            ["rrttl"] = Ttl.ToString(CultureInfo.InvariantCulture),
        };
    }
}
