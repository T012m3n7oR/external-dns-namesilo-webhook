using System.Collections.Generic;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class DeleteRecordRequest : INamesiloApiRequest
{
    required public string Domain { get; init; }

    required public string RecordId { get; init; }

    public IReadOnlyDictionary<string, string> ToQueryParameters()
    {
        return new Dictionary<string, string>
        {
            ["domain"] = Domain,
            ["rrid"] = RecordId,
        };
    }
}
