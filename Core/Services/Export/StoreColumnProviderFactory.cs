using Core.Contracts;
using Core.Enums;
using Core.Steam.Services.Export;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services.Export;

public class StoreColumnProviderFactory(IServiceProvider serviceProvider) : IStoreColumnProviderFactory
{
    public Task<IStoreColumnProvider> GetProviderAsync(Store store)
    {
        return Task.FromResult<IStoreColumnProvider>(store switch
        {
            Store.Steam => serviceProvider.GetRequiredService<SteamColumnProvider>(),
            _ => serviceProvider.GetRequiredService<DefaultColumnProvider>(),
        });
    }

    public Task<IStoreColumnProvider> GetProviderAsync()
    {
        return Task.FromResult<IStoreColumnProvider>(serviceProvider.GetRequiredService<DefaultColumnProvider>());
    }
}
