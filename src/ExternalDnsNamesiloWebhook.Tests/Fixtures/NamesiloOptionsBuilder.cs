using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using System;
using System.Security.Cryptography;

namespace ExternalDnsNamesiloWebhook.Tests.Fixtures;

internal sealed class NamesiloOptionsBuilder
{
    private string _apiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    private string[] _domainFilter = [];
    private int _defaultTtl = NamesiloRecordTtl.DefaultSeconds;
    private bool _dryRun;

    public static NamesiloOptionsBuilder New()
    {
        return new NamesiloOptionsBuilder();
    }

    public NamesiloOptionsBuilder WithApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    public NamesiloOptionsBuilder WithoutApiKey()
    {
        _apiKey = string.Empty;
        return this;
    }

    public NamesiloOptionsBuilder WithDomainFilter(params string[] domains)
    {
        _domainFilter = domains;
        return this;
    }

    public NamesiloOptionsBuilder WithDefaultTtl(int defaultTtl)
    {
        _defaultTtl = defaultTtl;
        return this;
    }

    public NamesiloOptionsBuilder WithDryRun(bool dryRun = true)
    {
        _dryRun = dryRun;
        return this;
    }

    public NamesiloOptions Build()
    {
        return new NamesiloOptions
        {
            ApiKey = _apiKey,
            DomainFilter = _domainFilter,
            DefaultTtl = _defaultTtl,
            DryRun = _dryRun,
        };
    }
}
