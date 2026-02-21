using DeviceBindingLib.Abstractions;
using DeviceBindingLib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeviceBindingLib.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeviceBindingLib(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceIdentityService, DeviceIdentityService>();
        services.AddSingleton<IDeviceBindingResolver, DeviceBindingResolver>();
        return services;
    }
}
