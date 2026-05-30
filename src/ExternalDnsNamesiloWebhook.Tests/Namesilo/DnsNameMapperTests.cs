using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Namesilo;

public class DnsNameMapperTests
{
    private readonly Fixture _fixture;

    public DnsNameMapperTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void ToRecordHost_Apex_ReturnsAt()
    {
        string domain = TestData.CreateDomain(_fixture);

        string host = DnsNameMapper.ToRecordHost(domain, domain);

        Assert.Equal(NamesiloDnsConstants.ApexRecordHost, host);
    }

    [Fact]
    public void ToRecordHost_Subdomain_ReturnsRelativeHost()
    {
        string domain = TestData.CreateDomain(_fixture);
        string label = TestData.CreateDomainLabel(_fixture);
        string dnsName = label + "." + domain;

        string host = DnsNameMapper.ToRecordHost(domain, dnsName);

        Assert.Equal(label, host);
    }

    [Fact]
    public void ToDnsName_ApexHost_ReturnsDomain()
    {
        string domain = TestData.CreateDomain(_fixture);

        string dnsName = DnsNameMapper.ToDnsName(domain, NamesiloDnsConstants.ApexRecordHost);

        Assert.Equal(domain, dnsName);
    }

    [Fact]
    public void FindDomainForDnsName_PrefersLongestMatch()
    {
        string parent = TestData.CreateDomain(_fixture);
        string child = TestData.CreateDomainLabel(_fixture) + "." + parent;
        string dnsName = TestData.CreateDomainLabel(_fixture) + "." + child;

        string? domain = DnsNameMapper.FindDomainForDnsName(dnsName, [parent, child]);

        Assert.Equal(child, domain);
    }

    [Fact]
    public void NormalizeDnsName_TrimsTrailingDot()
    {
        string domain = TestData.CreateDomain(_fixture);

        Assert.Equal(domain, DnsNameMapper.NormalizeDnsName(domain + "."));
    }

    [Fact]
    public void ToDnsName_RelativeHost_AppendsDomain()
    {
        string domain = TestData.CreateDomain(_fixture);
        string label = TestData.CreateDomainLabel(_fixture);

        string dnsName = DnsNameMapper.ToDnsName(domain, label);

        Assert.Equal(label + "." + domain, dnsName);
    }

    [Fact]
    public void DomainFilterMatches_SubdomainOfConfiguredDomain_ReturnsTrue()
    {
        string configured = TestData.CreateDomain(_fixture);
        string dnsName = TestData.CreateSubdomain(_fixture, configured);

        Assert.True(DnsNameMapper.DomainFilterMatches(dnsName, [configured]));
    }

    [Fact]
    public void DomainFilterMatches_UnrelatedDomain_ReturnsFalse()
    {
        string configured = TestData.CreateDomain(_fixture);
        string dnsName = TestData.CreateDomain(_fixture);

        Assert.False(DnsNameMapper.DomainFilterMatches(dnsName, [configured]));
    }

    [Theory]
    [InlineData(DnsRecordType.Unknown, false)]
    [InlineData(DnsRecordType.A, true)]
    [InlineData(DnsRecordType.AAAA, true)]
    [InlineData(DnsRecordType.CNAME, true)]
    [InlineData(DnsRecordType.TXT, true)]
    [InlineData(DnsRecordType.MX, true)]
    [InlineData(DnsRecordType.NS, true)]
    [InlineData(DnsRecordType.SRV, true)]
    [InlineData(DnsRecordType.CAA, false)]
    [InlineData(DnsRecordType.PTR, false)]
    [InlineData(DnsRecordType.SOA, false)]
    public void IsSupportedRecordType_ReturnsExpected(DnsRecordType recordType, bool expected)
    {
        Assert.Equal(expected, DnsNameMapper.IsSupportedRecordType(recordType));
    }

    [Fact]
    public void PrimaryTarget_ThrowsWhenTargetsEmpty()
    {
        Assert.Throws<NamesiloServiceException>(() => DnsNameMapper.PrimaryTarget([]));
    }

    [Fact]
    public void ToRecordHost_ThrowsWhenDnsNameOutsideDomain()
    {
        string domain = TestData.CreateDomain(_fixture);
        string otherDomain = TestData.CreateDomain(_fixture);

        Assert.Throws<NamesiloServiceException>(() => DnsNameMapper.ToRecordHost(domain, otherDomain));
    }

    [Fact]
    public void NormalizeRecordHost_ApexFqdn_ReturnsAt()
    {
        string domain = TestData.CreateDomain(_fixture);

        string host = DnsNameMapper.NormalizeRecordHost(domain, domain);

        Assert.Equal(NamesiloDnsConstants.ApexRecordHost, host);
    }

    [Fact]
    public void NormalizeRecordHost_SubdomainFqdn_ReturnsRelativeHost()
    {
        string domain = TestData.CreateDomain(_fixture);
        string label = TestData.CreateDomainLabel(_fixture);
        string fqdn = label + "." + domain;

        string host = DnsNameMapper.NormalizeRecordHost(domain, fqdn);

        Assert.Equal(label, host);
    }

    [Fact]
    public void NormalizeRecordHost_RelativeHost_ReturnsUnchanged()
    {
        string domain = TestData.CreateDomain(_fixture);
        string label = TestData.CreateDomainLabel(_fixture);

        string host = DnsNameMapper.NormalizeRecordHost(domain, label);

        Assert.Equal(label, host);
    }
}
