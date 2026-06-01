using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using System.Collections.Generic;

namespace ExternalDnsNamesiloWebhook.Tests.Fixtures;

internal static class DnsChangesBuilder
{
    public static DnsChanges CreateOnly(params DnsEndpoint[] endpoints)
    {
        return new DnsChanges
        {
            Create = [.. endpoints],
        };
    }

    public static DnsChanges DeleteOnly(params DnsEndpoint[] endpoints)
    {
        return new DnsChanges
        {
            Delete = [.. endpoints],
        };
    }

    public static DnsChanges Update(DnsEndpoint updateOld, DnsEndpoint updateNew)
    {
        return new DnsChanges
        {
            UpdateOld = [updateOld],
            UpdateNew = [updateNew],
        };
    }

    public static DnsChanges Empty()
    {
        return new DnsChanges
        {
            Create = [],
            UpdateOld = [],
            UpdateNew = [],
            Delete = [],
        };
    }
}
