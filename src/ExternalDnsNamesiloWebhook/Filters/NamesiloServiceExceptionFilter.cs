using ExternalDnsNamesiloWebhook.Core.Namesilo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace ExternalDnsNamesiloWebhook.Filters;

public sealed class NamesiloServiceExceptionFilter : IExceptionFilter
{
    private readonly ILogger<NamesiloServiceExceptionFilter> _logger;

    public NamesiloServiceExceptionFilter(ILogger<NamesiloServiceExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not NamesiloServiceException exception)
        {
            return;
        }

        _logger.LogError(context.Exception, "NameSilo service error: {Message}", exception.Message);
        context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
        context.ExceptionHandled = true;
    }
}
