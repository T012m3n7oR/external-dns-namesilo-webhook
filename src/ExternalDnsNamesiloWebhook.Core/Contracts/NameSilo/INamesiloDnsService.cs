using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;

namespace ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;

public interface INamesiloDnsService
{
    DomainFilterResponse GetDomainFilter();

    Task<IReadOnlyList<DnsEndpoint>> GetRecordsAsync(CancellationToken cancellationToken);

    Task ApplyChangesAsync(DnsChanges changes, CancellationToken cancellationToken);

    IReadOnlyList<DnsEndpoint> AdjustEndpoints(IReadOnlyList<DnsEndpoint> endpoints);
}
