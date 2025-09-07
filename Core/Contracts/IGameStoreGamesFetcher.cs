using Core.Models;

namespace Core.Contracts;

public interface IGameStoreGamesFetcher
{
    IAsyncEnumerable<Game> SearchGamesAsync(string query, CancellationToken cancellationToken = default);
}
