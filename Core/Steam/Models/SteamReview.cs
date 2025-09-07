using Core.Models;
using Newtonsoft.Json;

namespace Core.Steam.Models;

public class SteamReview : Review
{
    [JsonProperty("recommendationid")] public required string RecommendationId { get; set; }
    [JsonProperty("author")] public required SteamGameReviewAuthor Author { get; set; }
    [JsonProperty("language")] public required string Language { get; set; }
    [JsonProperty("review")] public override required string ReviewText { get; set; }
    [JsonProperty("timestamp_created")] public DateTime CreationDate { get; set; }
    [JsonProperty("timestamp_updated")] public DateTime UpdateDate { get; set; }
    [JsonProperty("voted_up")] public bool IsVotedUp { get; set; }
    [JsonProperty("votes_up")] public int VotesUpCount { get; set; }
    [JsonProperty("votes_funny")] public int VotesFunnyCount { get; set; }
    [JsonProperty("weighted_vote_score")] public double WeightedVoteScore { get; set; }
    [JsonProperty("comment_count")] public int CommentCount { get; set; }
    [JsonProperty("steam_purchase")] public bool SteamPurchase { get; set; }
    [JsonProperty("received_for_free")] public bool ReceivedForFree { get; set; }
    [JsonProperty("written_during_early_access")] public bool WrittenDuringEarlyAccess { get; set; }
    [JsonProperty("developer_response")] public string? DeveloperResponse { get; set; }
    [JsonProperty("timestamp_dev_responded")] public DateTime DeveloperResponseDate { get; set; }
    [JsonProperty("primarily_steam_deck")] public bool PrimarilySteamDeck { get; set; }
}
