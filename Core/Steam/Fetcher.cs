using Core.Steam.Enums;
using Core.Steam.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Core.Steam;

public class Fetcher
{
    private const string FetchGameReviewsBaseUrl = "https://store.steampowered.com/appreviews/";
    public static string ConstructFetchReviewsRequest(int gameId, Filter filter, Language[] languages, int dayRange, string cursor, ReviewType reviewType, PurchaseType purchaseType, int numPerPage, bool FilterOffTopicActivity)
    {
        string json = "1";
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters["json"] = json;
        parameters["filter"] = filter.ToString().ToLower();
        parameters["language"] = ConvertEnumArray(languages);
        if (filter == Filter.All)
        {
            parameters["day_range"] = Math.Min(Math.Max(0, dayRange), 365).ToString();
        }
        parameters["cursor"] = Uri.EscapeDataString(cursor);
        parameters["review_type"] = reviewType.ToString().ToLower();
        parameters["purchase_type"] = purchaseType.ToString().ToLower();
        parameters["num_per_page"] = Math.Min(Math.Max(0, numPerPage), 100).ToString();
        if (FilterOffTopicActivity)
        {
            parameters["filter_offtopic_activity"] = "0";
        }

        var query = new List<string>();
        foreach (var param in parameters)
        {
            query.Add($"{param.Key}={param.Value}");
        }
        return $"{Path.Combine(FetchGameReviewsBaseUrl, gameId.ToString())}?{string.Join('&', query)}";

        static string ConvertEnumArray<T>(T[] enums)
        {
            StringBuilder result = new("");
            for (int i = 0; i < enums.Length; i++)
            {
                result.Append(enums[i]?.ToString()?.ToLower());
                if (i + 1 != enums.Length)
                {
                    result.Append('+');
                }
            }
            return result.ToString();
        }
    }

    public static async IAsyncEnumerable<GameReview> Fetch(int gameId, Filter filter, Language[] languages, int dayRange, string cursor, ReviewType reviewType, PurchaseType purchaseType, int numPerPage, bool FilterOffTopicActivity, [EnumeratorCancellation] CancellationToken ct = default)
    {
        HttpClient client = new HttpClient();
        while (true)
        {
            var request = ConstructFetchReviewsRequest(gameId, filter, languages, dayRange, cursor, reviewType, purchaseType, numPerPage, FilterOffTopicActivity);
            Debug.WriteLine(request);
            var json = await client.GetStringAsync(request);
            var reviews = JsonConvert.DeserializeObject<GameReviews>(json, new UnixTimestampConverter());
            if (reviews?.Reviews == null || reviews.Reviews.Count == 0)
            {
                yield break;
            }
            foreach (var review in reviews?.Reviews ?? [])
            {
                yield return review;
            }
            break;
        }
    }

    public static GameReviewsQuerySummary FetchGameReviewsQuerySummary(int gameId, Filter filter, Language[] languages, int dayRange, string cursor, ReviewType reviewType, PurchaseType purchaseType, int numPerPage, bool FilterOffTopicActivity)
    {
        return new();
    }

    public static void FetchGame(int gameId)
    {

    }

    public static void SearchForGame(string name)
    {

    }
}