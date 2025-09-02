namespace Core.Steam.Models;

public record class GameReviewAuthor
{
    public string SteamId { get; set; }
    public int NumGamesOwned { get; set; }
    public int NumReviews { get; set; }
    public long PlayTimeForever { get; set; }
    public long PlayTimeLastTwoWeeks { get; set; }
    public long PlayTimeAtReview { get; set; }
    public DateTime LastPlayed { get; set; }
}