using Core.Contracts;
using Core.Models;

namespace Core.Steam.Interfaces;

public interface ISteamStoreGamesFetcher : IGameStoreGamesFetcher
{
    IAsyncEnumerable<Game> DeepSearchGamesAsync(string query, CancellationToken cancellationToken = default);
}

