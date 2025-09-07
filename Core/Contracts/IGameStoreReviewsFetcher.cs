using Core.Models;

namespace Core.Contracts;

public interface IGameStoreReviewsFetcher
{
    Task<ReviewsSummary?> FetchReviewsSummaryAsync(string gameId, ReviewQueryParameters parameters, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Review> FetchReviewsAsync(string gameId, ReviewQueryParameters parameters, CancellationToken cancellationToken = default);
}
