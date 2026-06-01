using System;
using System.Collections.Generic;
using System.Linq;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using Microsoft.Extensions.Logging;

namespace ExternalDnsNamesiloWebhook.Core.Logging;

public static class DnsEndpointLogging
{
    /// <summary>
    /// Substring used to identify ExternalDNS TXT ownership records in sync summaries.
    /// Matches the common <c>--txt-prefix=external-dns-</c> convention.
    /// </summary>
    public const string ExternalDnsOwnershipTxtMarker = "external-dns";

    public static bool IsNotableForSync(DnsEndpoint endpoint, IEnumerable<string> domainFilters)
    {
        if (endpoint.RecordType is DnsRecordType.A or DnsRecordType.AAAA)
        {
            return DnsNameMapper.DomainFilterMatches(endpoint.DnsName, domainFilters);
        }

        if (endpoint.RecordType == DnsRecordType.TXT)
        {
            return endpoint.DnsName.Contains(
                ExternalDnsOwnershipTxtMarker,
                StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static void LogSyncSummary(
        ILogger logger,
        string label,
        IReadOnlyList<DnsEndpoint> endpoints,
        IEnumerable<string> domainFilters)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        logger.LogInformation("{Label}: {EndpointCount} DNS endpoint(s)", label, endpoints.Count);

        List<DnsEndpoint> notable = endpoints
            .Where(endpoint => IsNotableForSync(endpoint, domainFilters))
            .OrderBy(static endpoint => endpoint.DnsName)
            .ThenBy(static endpoint => endpoint.RecordType)
            .ToList();

        foreach (DnsEndpoint endpoint in notable)
        {
            logger.LogInformation(
                "{Label} notable: {RecordType} {DnsName} -> {Target} ttl={RecordTtl}",
                label,
                endpoint.RecordType,
                endpoint.DnsName,
                FormatTarget(endpoint),
                endpoint.RecordTtl > 0 ? endpoint.RecordTtl.ToString() : "default");
        }

        int omitted = endpoints.Count - notable.Count;
        if (omitted > 0 && logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "{Label}: {OmittedCount} other endpoint(s) omitted from Information summary",
                label,
                omitted);
        }
    }

    public static void LogEndpointSet(
        ILogger logger,
        LogLevel level,
        string label,
        IReadOnlyList<DnsEndpoint> endpoints)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        logger.Log(level, "{Label}: {EndpointCount} DNS endpoint(s)", label, endpoints.Count);

        foreach (DnsEndpoint endpoint in endpoints.OrderBy(static e => e.DnsName).ThenBy(static e => e.RecordType))
        {
            logger.Log(
                level,
                "{Label} record: {RecordType} {DnsName} -> {Target} ttl={RecordTtl}",
                label,
                endpoint.RecordType,
                endpoint.DnsName,
                FormatTarget(endpoint),
                endpoint.RecordTtl > 0 ? endpoint.RecordTtl.ToString() : "default");
        }
    }

    private static string FormatTarget(DnsEndpoint endpoint)
    {
        if (endpoint.Targets.Count == 0)
        {
            return "(no targets)";
        }

        string target = DnsNameMapper.PrimaryTarget(endpoint.Targets);
        return DnsLogRedaction.FormatRecordTarget(endpoint.RecordType, target);
    }
}
