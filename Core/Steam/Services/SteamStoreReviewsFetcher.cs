using Core.Steam.Enums;
using Core.Steam.Models;
using Core.Contracts;
using Core.Models;
using Newtonsoft.Json;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Core.Steam.Services;

public class SteamStoreReviewsFetcher(HttpClient httpClient, ILogger<SteamStoreReviewsFetcher> logger) : IGameStoreReviewsFetcher, IDisposable
{
    private const string FetchGameReviewsBaseUrl = "https://store.steampowered.com/appreviews/";

    public async Task<ReviewsSummary?> FetchReviewsSummaryAsync(string gameId, ReviewQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SteamReviewQueryParameters steamParams)
        {
            throw new ArgumentException("Steam store requires SteamReviewQueryParameters", nameof(parameters));
        }
        
        steamParams = NormalizeSteamParameters(steamParams);
        steamParams.ReviewType = ReviewType.All;
        steamParams.Filter = Filter.Recent;
        try
        {
            var request = ConstructFetchReviewsRequest(gameId, steamParams, getSummaryOnly: true);
            logger.LogInformation("Fetching Steam reviews summary: {Request}", request);

            var json = await httpClient.GetStringAsync(request, cancellationToken);
            var deserialized = JsonConvert.DeserializeObject<SteamGameReviewQuueryResult>(json, new UnixTimestampConverter()) ?? new SteamGameReviewQuueryResult();
            return deserialized.QuerySummary;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP error fetching Steam reviews summary: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to fetch reviews summary from Steam API", ex);
        }
        catch (JsonException ex)
        {
            logger.LogError("JSON parsing error: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to parse Steam API response", ex);
        }
    }

    public async IAsyncEnumerable<Review> FetchReviewsAsync(string gameId, ReviewQueryParameters parameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (parameters is not SteamReviewQueryParameters steamParams)
        {
            throw new ArgumentException("Steam store requires SteamReviewQueryParameters", nameof(parameters));
        }

        // Normalize parameters to ensure strict Steam API compliance
        steamParams = NormalizeSteamParameters(steamParams);
        ValidateSteamParameters(gameId, steamParams);

        string cursor = steamParams.Cursor;

        while (!cancellationToken.IsCancellationRequested)
        {
            var reviewsResponse = await FetchReviewsFromApi(gameId, steamParams, cursor, cancellationToken);
            
            if (reviewsResponse?.Reviews == null || reviewsResponse.Reviews.Count == 0)
            {
                yield break;
            }

            foreach (var review in reviewsResponse.Reviews.TakeWhile(review => !cancellationToken.IsCancellationRequested))
            {
                yield return review;
            }
            
            cursor = reviewsResponse.Cursor ?? "*";
        }
    }

    private async Task<SteamGameReviewQuueryResult> FetchReviewsFromApi(string gameId, SteamReviewQueryParameters parameters, string cursor = "*", CancellationToken cancellationToken = default)
    {
        try
        {
            var request = ConstructFetchReviewsRequest(gameId, parameters, cursor);
            logger.LogInformation("Fetching Steam reviews: {Request}", request);

            var json = await httpClient.GetStringAsync(request, cancellationToken);
            var deserialized = JsonConvert.DeserializeObject<SteamGameReviewQuueryResult>(json, new UnixTimestampConverter()) ?? new SteamGameReviewQuueryResult();
            return deserialized;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("HTTP error fetching Steam reviews: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to fetch reviews from Steam API", ex);
        }
        catch (JsonException ex)
        {
            logger.LogError("JSON parsing error: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to parse Steam API response", ex);
        }
    }

    private static SteamReviewQueryParameters NormalizeSteamParameters(SteamReviewQueryParameters parameters)
    {
        var normalized = new SteamReviewQueryParameters
        {
            Filter = parameters.Filter,
            ReviewType = parameters.ReviewType,
            PurchaseType = parameters.PurchaseType,
            FilterOffTopicActivity = parameters.FilterOffTopicActivity,
            Cursor = parameters.Cursor,
            NumPerPage = Math.Min(Math.Max(1, parameters.NumPerPage), 100),
            // DayRange removed as it was only used with Filter.All
        };

        if (parameters.Languages is { Length: > 0 })
        {
            normalized.Languages = parameters.Languages;
        }

        return normalized;
    }

    private static void ValidateSteamParameters(string gameId, SteamReviewQueryParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            throw new ArgumentException("GameId cannot be null or empty", nameof(parameters));
        }

        if (!int.TryParse(gameId, out _))
        {
            throw new ArgumentException("GameId must be a valid integer", nameof(parameters));
        }

        if (parameters.NumPerPage is <= 0 or > 100)
        {
            throw new ArgumentException("NumPerPage must be between 1 and 100", nameof(parameters));
        }

        // Day range validation removed as Filter.All and DayRange are no longer supported
    }

    private static string ConstructFetchReviewsRequest(string gameId, SteamReviewQueryParameters parameters, string cursor = "*", bool getSummaryOnly = false)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["json"] = "1",
            ["filter"] = parameters.Filter.ToString().ToLower(),
            ["language"] = ConvertEnumArray(parameters.Languages),
            ["cursor"] = Uri.EscapeDataString(cursor),
            ["review_type"] = parameters.ReviewType.ToString().ToLower(),
            ["purchase_type"] = parameters.PurchaseType.ToString().ToLower(),
            // day_range parameter removed as Filter.All is no longer supported
            ["filter_offtopic_activity"] =  parameters.FilterOffTopicActivity ? string.Empty : "0",
            ["num_per_page"] = getSummaryOnly ? "0" : Math.Min(Math.Max(1, parameters.NumPerPage), 100).ToString()
        };
        

        var queryString = string.Join("&", queryParams.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value)).Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{FetchGameReviewsBaseUrl}{gameId}?{queryString}";
    }

    private static string ConvertEnumArray<T>(T[]? enums)
    {
        if (enums is null || enums.Length == 0)
        {
            return "all";
        }

        var result = new StringBuilder();
        for (int i = 0; i < enums.Length; i++)
        {
            result.Append(enums[i]?.ToString()?.ToLower());
            if (i < enums.Length - 1)
            {
                result.Append('+');
            }
        }
        return result.ToString();
    }

    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}