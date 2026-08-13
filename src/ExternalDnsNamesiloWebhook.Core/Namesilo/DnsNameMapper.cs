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
            if (string.Equals(normalizedDnsName, normalizedDomain, StringComparison.Ordinal) ||
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

        if (string.Equals(normalizedDnsName, normalizedDomain, StringComparison.Ordinal))
        {
            return NamesiloDns.ApexRecordHost;
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
            || string.Equals(normalizedHost, NamesiloDns.ApexRecordHost, StringComparison.Ordinal)
            || string.Equals(normalizedHost, normalizedDomain, StringComparison.Ordinal))
        {
            return normalizedDomain;
        }

        // NameSilo may return an FQDN (ends with the zone) or a relative host.
        // Multi-label relatives like "registry.gitlab" contain dots but are not FQDNs.
        if (normalizedHost.EndsWith('.' + normalizedDomain, StringComparison.Ordinal))
        {
            return normalizedHost;
        }

        return normalizedHost + "." + normalizedDomain;
    }

    public static string NormalizeRecordHost(string domain, string host)
    {
        string normalizedHost = host.Trim().TrimEnd('.').ToLowerInvariant();
        string normalizedDomain = NormalizeDnsName(domain);

        if (string.Equals(normalizedHost, normalizedDomain, StringComparison.Ordinal))
        {
            return NamesiloDns.ApexRecordHost;
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
