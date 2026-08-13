using System.Collections.Generic;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class ListRecordsRequest : INamesiloApiRequest
{
    required public string Domain { get; init; }

    public IReadOnlyDictionary<string, string> ToQueryParameters()
    {
        return new Dictionary<string, string>(System.StringComparer.Ordinal) { ["domain"] = Domain };
    }
}
