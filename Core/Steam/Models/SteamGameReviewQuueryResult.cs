using Newtonsoft.Json;

namespace Core.Steam.Models;

public class SteamGameReviewQuueryResult
{
    [JsonProperty("success")] public int Success { get; set; }
    [JsonProperty("query_summary")] public SteamGameReviewsQuerySummary? QuerySummary { get; set; } = null;
    [JsonProperty("reviews")] public List<SteamReview> Reviews { get; set; } = [];
    [JsonProperty("cursor")] public string Cursor { get; set; } = "*";
}