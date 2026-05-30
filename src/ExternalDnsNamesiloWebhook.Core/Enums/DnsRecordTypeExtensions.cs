namespace ExternalDnsNamesiloWebhook.Core.Enums;

public static class DnsRecordTypeExtensions
{
    public static bool IsSupported(this DnsRecordType recordType)
    {
        return recordType is DnsRecordType.A
            or DnsRecordType.AAAA
            or DnsRecordType.CNAME
            or DnsRecordType.TXT
            or DnsRecordType.MX
            or DnsRecordType.NS
            or DnsRecordType.SRV;
    }
}
