using Newtonsoft.Json;

namespace Core.Steam.Models;

public class SteamGameReviewAuthor
{
    [JsonProperty("steamid")] public required string SteamId { get; set; }
    [JsonProperty("num_games_owned")] public int GamesOwnedCount { get; set; }
    [JsonProperty("num_reviews")] public int ReviewsCount { get; set; }
    [JsonProperty("playtime_forever")] public long PlayTimeForever { get; set; }
    [JsonProperty("playtime_last_two_weeks")] public long PlayTimeLastTwoWeeks { get; set; }
    [JsonProperty("playtime_at_review")] public long PlayTimeAtReview { get; set; }
    [JsonProperty("deck_playtime_at_review")] public long DeckPlayTimeAtReview { get; set; }
    [JsonProperty("last_played")] public DateTime LastPlayed { get; set; }
}