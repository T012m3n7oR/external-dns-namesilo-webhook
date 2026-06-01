using System;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExternalDnsNamesiloWebhook.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNamesiloWebhookCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NamesiloOptions>(configuration.GetSection(NamesiloOptions.SectionName));
        services.AddSingleton<IPostConfigureOptions<NamesiloOptions>, NamesiloOptionsPostConfigure>();
        services.AddHttpClient(HttpClientNames.NameSilo)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                NamesiloOptions options = serviceProvider.GetRequiredService<IOptions<NamesiloOptions>>().Value;
                client.BaseAddress = new Uri(NormalizeApiBaseUrl(options.ApiBaseUrl ?? NamesiloApiDefaults.BaseUrl));
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        services.AddScoped<INamesiloApiClient, NamesiloApiClient>();
        services.AddScoped<INamesiloDnsService, NamesiloDnsService>();
        DependencyInjectionScopingValidator.Validate(services);
        return services;
    }

    internal static string NormalizeApiBaseUrl(string apiBaseUrl)
    {
        return apiBaseUrl.TrimEnd('/') + "/";
    }
}
