using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

namespace ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;

public interface INamesiloApiClient
{
    Task<IReadOnlyList<NamesiloDnsRecord>> ListRecordsAsync(
        ListRecordsRequest request,
        CancellationToken cancellationToken);

    Task<string> AddRecordAsync(
        AddRecordRequest request,
        CancellationToken cancellationToken);

    Task UpdateRecordAsync(
        UpdateRecordRequest request,
        CancellationToken cancellationToken);

    Task DeleteRecordAsync(
        DeleteRecordRequest request,
        CancellationToken cancellationToken);
}
