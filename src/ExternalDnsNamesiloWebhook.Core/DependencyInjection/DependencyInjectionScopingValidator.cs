using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ExternalDnsNamesiloWebhook.Core.DependencyInjection;

/// <summary>
/// Validates that no singleton registration has a constructor dependency on a scoped or transient service.
/// A singleton that captures a shorter-lived dependency would use one instance for the app lifetime,
/// breaking scope isolation and transient semantics.
/// </summary>
public static class DependencyInjectionScopingValidator
{
    public static void Validate(IServiceCollection services)
    {
        Dictionary<Type, ServiceLifetime> serviceTypeToLifetime = BuildServiceTypeLifetimeMap(services);
        List<string> violations = [];

        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.Lifetime != ServiceLifetime.Singleton)
            {
                continue;
            }

            Type? implementationType = descriptor.ImplementationType;
            if (implementationType is null)
            {
                continue;
            }

            ConstructorInfo? ctor = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault();
            if (ctor is null)
            {
                continue;
            }

            foreach (ParameterInfo parameter in ctor.GetParameters())
            {
                Type paramType = parameter.ParameterType;
                if (TryGetDependencyLifetime(paramType, serviceTypeToLifetime, out ServiceLifetime dependencyLifetime)
                    && dependencyLifetime != ServiceLifetime.Singleton)
                {
                    violations.Add(
                        $"Singleton {implementationType.Name} has constructor dependency on {dependencyLifetime.ToString().ToLowerInvariant()} service {paramType.Name}.");
                }
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "DI scoping violations (singleton must not depend on scoped or transient): "
                + string.Join(" ", violations));
        }
    }

    private static Dictionary<Type, ServiceLifetime> BuildServiceTypeLifetimeMap(IServiceCollection services)
    {
        Dictionary<Type, ServiceLifetime> map = [];

        foreach (ServiceDescriptor descriptor in services)
        {
            Type serviceType = descriptor.ServiceType;
            if (serviceType.IsGenericTypeDefinition)
            {
                continue;
            }

            if (!map.TryGetValue(serviceType, out ServiceLifetime existing))
            {
                map[serviceType] = descriptor.Lifetime;
            }
            else
            {
                int minLifetime = Math.Min((int)existing, (int)descriptor.Lifetime);
                map[serviceType] = (ServiceLifetime)minLifetime;
            }
        }

        return map;
    }

    private static bool TryGetDependencyLifetime(
        Type parameterType,
        Dictionary<Type, ServiceLifetime> serviceTypeToLifetime,
        out ServiceLifetime lifetime)
    {
        lifetime = default;

        if (parameterType.IsGenericType && parameterType.ContainsGenericParameters)
        {
            return false;
        }

        if (serviceTypeToLifetime.TryGetValue(parameterType, out lifetime))
        {
            return true;
        }

        foreach (KeyValuePair<Type, ServiceLifetime> entry in serviceTypeToLifetime)
        {
            if (entry.Key.IsGenericTypeDefinition && parameterType.IsConstructedGenericType)
            {
                if (parameterType.GetGenericTypeDefinition() == entry.Key)
                {
                    lifetime = entry.Value;
                    return true;
                }
            }
        }

        return false;
    }
}
