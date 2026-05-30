using System.Collections.Generic;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public interface INamesiloApiRequest
{
    IReadOnlyDictionary<string, string> ToQueryParameters();
}
