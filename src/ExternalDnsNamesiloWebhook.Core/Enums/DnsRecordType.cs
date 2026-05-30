namespace ExternalDnsNamesiloWebhook.Core.Enums;

/// <summary>
/// NameSilo DNS resource record types returned by the API (<c>type</c> / <c>rrtype</c>).
/// Values match NameSilo / ExternalDNS wire strings (e.g. <c>A</c>, <c>TXT</c>).
/// </summary>
public enum DnsRecordType
{
    /// <summary>Unset or missing record type (not a NameSilo wire value).</summary>
    Unknown = 0,

    /// <summary>IPv4 address record.</summary>
    A,

    /// <summary>IPv6 address record.</summary>
    AAAA,

    /// <summary>Certification Authority Authorization record.</summary>
    CAA,

    /// <summary>Canonical name record.</summary>
    CNAME,

    /// <summary>Mail exchange record.</summary>
    MX,

    /// <summary>Name server record.</summary>
    NS,

    /// <summary>Pointer record for reverse DNS.</summary>
    PTR,

    /// <summary>Start of authority record.</summary>
    SOA,

    /// <summary>Service locator record.</summary>
    SRV,

    /// <summary>Text record.</summary>
    TXT,
}
