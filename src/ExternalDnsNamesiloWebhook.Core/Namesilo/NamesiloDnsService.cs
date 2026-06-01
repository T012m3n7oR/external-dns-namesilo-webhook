using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Logging;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

public sealed class NamesiloDnsService : INamesiloDnsService
{
    private readonly INamesiloApiClient _apiClient;
    private readonly IOptions<NamesiloOptions> _options;
    private readonly ILogger<NamesiloDnsService> _logger;

    public NamesiloDnsService(
        INamesiloApiClient apiClient,
        IOptions<NamesiloOptions> options,
        ILogger<NamesiloDnsService> logger)
    {
        _apiClient = apiClient;
        _options = options;
        _logger = logger;
    }

    public DomainFilterResponse GetDomainFilter()
    {
        return new DomainFilterResponse
        {
            Filters = _options.Value.DomainFilter.ToArray(),
        };
    }

    public async Task<IReadOnlyList<DnsEndpoint>> GetRecordsAsync(CancellationToken cancellationToken)
    {
        List<DnsEndpoint> endpoints = [];

        foreach (string domain in _options.Value.DomainFilter)
        {
            IReadOnlyList<NamesiloDnsRecord> records = await _apiClient.ListRecordsAsync(
                new ListRecordsRequest { Domain = domain },
                cancellationToken).ConfigureAwait(false);

            foreach (NamesiloDnsRecord record in records)
            {
                if (!record.RecordType.IsSupported())
                {
                    _logger.LogDebug(
                        "Skipping unsupported NameSilo record type {RecordType} in {Domain}",
                        record.RecordType,
                        domain);
                    continue;
                }

                DnsEndpoint endpoint = ToEndpoint(domain, record);
                if (DnsNameMapper.DomainFilterMatches(endpoint.DnsName, _options.Value.DomainFilter))
                {
                    endpoints.Add(endpoint);
                }
            }
        }

        string[] domainFilters = _options.Value.DomainFilter;
        DnsEndpointLogging.LogSyncSummary(
            _logger,
            "NameSilo zone state returned to ExternalDNS",
            endpoints,
            domainFilters);
        DnsEndpointLogging.LogEndpointSet(
            _logger,
            LogLevel.Debug,
            "NameSilo zone state (full)",
            endpoints);

        return endpoints;
    }

    public async Task ApplyChangesAsync(DnsChanges changes, CancellationToken cancellationToken)
    {
        int createCount = changes.Create.Count;
        int updateCount = changes.UpdateNew.Count;
        int deleteCount = changes.Delete.Count;

        if (createCount == 0 && updateCount == 0 && deleteCount == 0)
        {
            _logger.LogDebug("ApplyChanges: empty change set (no NameSilo API mutations)");
            return;
        }

        _logger.LogInformation(
            "Applying changes create={Create} update={Update} delete={Delete}",
            createCount,
            updateCount,
            deleteCount);

        DnsEndpointLogging.LogEndpointSet(
            _logger,
            LogLevel.Debug,
            "ApplyChanges create",
            changes.Create);
        DnsEndpointLogging.LogEndpointSet(
            _logger,
            LogLevel.Debug,
            "ApplyChanges delete",
            changes.Delete);

        for (int index = 0; index < updateCount; index++)
        {
            DnsEndpoint updateNew = changes.UpdateNew[index];
            DnsEndpoint updateOld = index < changes.UpdateOld.Count
                ? changes.UpdateOld[index]
                : updateNew;

            _logger.LogInformation(
                "ApplyChanges update: {RecordType} {DnsName} {OldTarget} -> {NewTarget}",
                updateNew.RecordType,
                updateNew.DnsName,
                FormatTarget(updateOld),
                FormatTarget(updateNew));
        }

        foreach (DnsEndpoint endpoint in changes.Create)
        {
            await CreateRecordAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }

        for (int index = 0; index < changes.UpdateNew.Count; index++)
        {
            DnsEndpoint updateNew = changes.UpdateNew[index];
            DnsEndpoint updateOld = index < changes.UpdateOld.Count
                ? changes.UpdateOld[index]
                : updateNew;

            await UpdateRecordAsync(updateOld, updateNew, cancellationToken).ConfigureAwait(false);
        }

        foreach (DnsEndpoint endpoint in changes.Delete)
        {
            await DeleteRecordAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogDebug("Applied DNS change batch successfully");
    }

    public IReadOnlyList<DnsEndpoint> AdjustEndpoints(IReadOnlyList<DnsEndpoint> endpoints)
    {
        string[] domainFilters = _options.Value.DomainFilter;
        DnsEndpointLogging.LogSyncSummary(
            _logger,
            "ExternalDNS desired endpoints (from cluster)",
            endpoints,
            domainFilters);
        DnsEndpointLogging.LogEndpointSet(
            _logger,
            LogLevel.Debug,
            "ExternalDNS desired endpoints (from cluster, full)",
            endpoints);

        NamesiloOptions options = _options.Value;
        List<DnsEndpoint> adjusted = [];

        foreach (DnsEndpoint endpoint in endpoints)
        {
            DnsEndpoint copy = new()
            {
                DnsName = endpoint.DnsName,
                RecordType = endpoint.RecordType,
                SetIdentifier = endpoint.SetIdentifier,
                Labels = endpoint.Labels,
                ProviderSpecific = endpoint.ProviderSpecific,
                Targets = endpoint.Targets.ToList(),
                RecordTtl = NamesiloRecordTtl.Normalize(
                    endpoint.RecordTtl > 0 ? (int)endpoint.RecordTtl : options.DefaultTtl),
            };

            adjusted.Add(copy);
        }

        DnsEndpointLogging.LogEndpointSet(
            _logger,
            LogLevel.Debug,
            "ExternalDNS desired endpoints (after TTL adjust)",
            adjusted);

        return adjusted;
    }

    private static string FormatTarget(DnsEndpoint endpoint)
    {
        if (endpoint.Targets.Count == 0)
        {
            return "(no targets)";
        }

        return DnsLogRedaction.FormatRecordTarget(
            endpoint.RecordType,
            DnsNameMapper.PrimaryTarget(endpoint.Targets));
    }

    private async Task CreateRecordAsync(DnsEndpoint endpoint, CancellationToken cancellationToken)
    {
        string domain = RequireDomain(endpoint.DnsName);
        string host = DnsNameMapper.ToRecordHost(domain, endpoint.DnsName);
        int ttl = ResolveTtl(endpoint.RecordTtl);

        _logger.LogInformation(
            "Creating {Type} {DnsName} -> {Target}",
            endpoint.RecordType,
            endpoint.DnsName,
            DnsLogRedaction.FormatRecordTarget(endpoint.RecordType, DnsNameMapper.PrimaryTarget(endpoint.Targets)));

        await _apiClient.AddRecordAsync(
            new AddRecordRequest
            {
                Domain = domain,
                RecordType = endpoint.RecordType,
                RecordHost = host,
                RecordValue = DnsNameMapper.PrimaryTarget(endpoint.Targets),
                Ttl = ttl,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateRecordAsync(
        DnsEndpoint updateOld,
        DnsEndpoint updateNew,
        CancellationToken cancellationToken)
    {
        string domain = RequireDomain(updateNew.DnsName);
        NamesiloDnsRecord existing = await FindRecordAsync(domain, updateOld, cancellationToken)
            .ConfigureAwait(false);

        string host = DnsNameMapper.ToRecordHost(domain, updateNew.DnsName);
        int ttl = ResolveTtl(updateNew.RecordTtl);

        _logger.LogInformation(
            "Updating {Type} {DnsName} -> {Target}",
            updateNew.RecordType,
            updateNew.DnsName,
            DnsLogRedaction.FormatRecordTarget(updateNew.RecordType, DnsNameMapper.PrimaryTarget(updateNew.Targets)));

        await _apiClient.UpdateRecordAsync(
            new UpdateRecordRequest
            {
                Domain = domain,
                RecordId = existing.RecordId,
                RecordHost = host,
                RecordValue = DnsNameMapper.PrimaryTarget(updateNew.Targets),
                Ttl = ttl,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteRecordAsync(DnsEndpoint endpoint, CancellationToken cancellationToken)
    {
        string domain = RequireDomain(endpoint.DnsName);
        NamesiloDnsRecord existing = await FindRecordAsync(domain, endpoint, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Deleting {Type} {DnsName}", endpoint.RecordType, endpoint.DnsName);

        await _apiClient.DeleteRecordAsync(
            new DeleteRecordRequest { Domain = domain, RecordId = existing.RecordId },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<NamesiloDnsRecord> FindRecordAsync(
        string domain,
        DnsEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NamesiloDnsRecord> records = await _apiClient.ListRecordsAsync(
            new ListRecordsRequest { Domain = domain },
            cancellationToken).ConfigureAwait(false);

        string expectedHost = DnsNameMapper.ToRecordHost(domain, endpoint.DnsName);
        string expectedValue = DnsNameMapper.PrimaryTarget(endpoint.Targets);

        foreach (NamesiloDnsRecord record in records)
        {
            if (record.RecordType != endpoint.RecordType)
            {
                continue;
            }

            string recordHost = DnsNameMapper.NormalizeRecordHost(domain, record.Host);
            if (!string.Equals(recordHost, expectedHost, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(record.Value, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return record;
        }

        throw new NamesiloServiceException(
            $"Could not find NameSilo record for {endpoint.RecordType} {endpoint.DnsName} -> {expectedValue}.");
    }

    private DnsEndpoint ToEndpoint(string domain, NamesiloDnsRecord record)
    {
        if (!record.RecordType.IsSupported())
        {
            throw new NamesiloServiceException($"Unsupported NameSilo record type '{record.RecordType}'.");
        }

        return new DnsEndpoint
        {
            DnsName = DnsNameMapper.ToDnsName(domain, record.Host),
            RecordType = record.RecordType,
            RecordTtl = record.Ttl,
            Targets = [record.Value],
        };
    }

    private string RequireDomain(string dnsName)
    {
        string? domain = DnsNameMapper.FindDomainForDnsName(dnsName, _options.Value.DomainFilter);
        if (domain == null)
        {
            throw new NamesiloServiceException($"DNS name '{dnsName}' is outside configured domain filters.");
        }

        return domain;
    }

    private int ResolveTtl(long recordTtl)
    {
        int ttl = recordTtl > 0 ? (int)recordTtl : _options.Value.DefaultTtl;
        return NamesiloRecordTtl.Normalize(ttl);
    }
}
