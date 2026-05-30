using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ExternalDnsNamesiloWebhook.DependencyInjection;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddWebhookControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<NamesiloServiceExceptionFilter>();
        builder.Services.AddControllers(options =>
            {
                options.Filters.AddService<NamesiloServiceExceptionFilter>();
            })
            .AddJsonOptions(options => WebhookJson.ApplyTo(options.JsonSerializerOptions));

        return builder;
    }
}
