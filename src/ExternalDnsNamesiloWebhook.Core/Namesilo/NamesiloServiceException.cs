using System;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

public sealed class NamesiloServiceException : Exception
{
    public NamesiloServiceException(string message)
        : base(message)
    {
    }

    public NamesiloServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
