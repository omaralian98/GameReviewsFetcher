using Core.Models;
using Newtonsoft.Json;

namespace Core.Steam.Models;

public class SteamGameReviewsQuerySummary : ReviewsSummary
{
    [JsonProperty("num_reviews")] public int NumReviews { get; set; }
    [JsonProperty("review_score")] public int ReviewScore { get; set; }
    [JsonProperty("review_score_desc")] public string ReviewScoreDescription { get; set; } = string.Empty;
    [JsonProperty("total_positive")] public int TotalPositive { get; set; }
    [JsonProperty("total_negative")] public int TotalNegative { get; set; }
    [JsonProperty("total_reviews")] public new int TotalReviews { get; set; }
}