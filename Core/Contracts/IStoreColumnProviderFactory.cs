using Core.Enums;

namespace Core.Contracts;

public interface IStoreColumnProviderFactory
{
    Task<IStoreColumnProvider> GetProviderAsync(Store store);
    Task<IStoreColumnProvider> GetProviderAsync();
}
