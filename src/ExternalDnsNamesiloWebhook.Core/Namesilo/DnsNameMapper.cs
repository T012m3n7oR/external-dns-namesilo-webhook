using System;
using System.Collections.Generic;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Enums;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

public static class DnsNameMapper
{
    public static bool IsSupportedRecordType(DnsRecordType recordType)
    {
        return recordType.IsSupported();
    }

    public static string NormalizeDnsName(string dnsName)
    {
        return dnsName.Trim().TrimEnd('.').ToLowerInvariant();
    }

    public static string? FindDomainForDnsName(string dnsName, IEnumerable<string> domains)
    {
        string normalizedDnsName = NormalizeDnsName(dnsName);
        string? bestMatch = null;

        foreach (string domain in domains)
        {
            string normalizedDomain = NormalizeDnsName(domain);
            if (normalizedDnsName == normalizedDomain ||
                normalizedDnsName.EndsWith('.' + normalizedDomain, StringComparison.Ordinal))
            {
                if (bestMatch == null || normalizedDomain.Length > bestMatch.Length)
                {
                    bestMatch = normalizedDomain;
                }
            }
        }

        return bestMatch;
    }

    public static string ToRecordHost(string domain, string dnsName)
    {
        string normalizedDomain = NormalizeDnsName(domain);
        string normalizedDnsName = NormalizeDnsName(dnsName);

        if (normalizedDnsName == normalizedDomain)
        {
            return NamesiloDnsConstants.ApexRecordHost;
        }

        string suffix = "." + normalizedDomain;
        if (normalizedDnsName.EndsWith(suffix, StringComparison.Ordinal))
        {
            return normalizedDnsName[..^suffix.Length];
        }

        throw new NamesiloServiceException($"DNS name '{dnsName}' is not under domain '{domain}'.");
    }

    public static string ToDnsName(string domain, string host)
    {
        string normalizedDomain = NormalizeDnsName(domain);
        string normalizedHost = host.Trim().TrimEnd('.').ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedHost)
            || normalizedHost == NamesiloDnsConstants.ApexRecordHost
            || normalizedHost == normalizedDomain)
        {
            return normalizedDomain;
        }

        if (normalizedHost.Contains('.', StringComparison.Ordinal))
        {
            return NormalizeDnsName(normalizedHost);
        }

        return normalizedHost + "." + normalizedDomain;
    }

    public static string NormalizeRecordHost(string domain, string host)
    {
        string normalizedHost = host.Trim().TrimEnd('.').ToLowerInvariant();
        string normalizedDomain = NormalizeDnsName(domain);

        if (normalizedHost == normalizedDomain)
        {
            return NamesiloDnsConstants.ApexRecordHost;
        }

        if (normalizedHost.EndsWith('.' + normalizedDomain, StringComparison.Ordinal))
        {
            return normalizedHost[..^(normalizedDomain.Length + 1)];
        }

        return normalizedHost;
    }

    public static bool DomainFilterMatches(string dnsName, IEnumerable<string> domains)
    {
        return FindDomainForDnsName(dnsName, domains) != null;
    }

    public static string PrimaryTarget(IReadOnlyList<string> targets)
    {
        if (targets.Count == 0)
        {
            throw new NamesiloServiceException("Endpoint must include at least one target.");
        }

        return targets[0];
    }
}
