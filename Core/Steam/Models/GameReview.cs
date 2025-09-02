namespace Core.Steam.Models;

public record class GameReview
{
    public string RecommendationId { get; set; }
    public GameReviewAuthor Author { get; set; }
    public string Language { get; set; }
    public string Review { get; set; }
    public DateTime TimeStampCreated { get; set; }
    public DateTime TimeStampUpdated { get; set; }
    public bool VotedUp { get; set; }
    public int VotesUp { get; set; }
    public int VotesFunny { get; set; }
    public double WeightedVoteScore { get; set; }
    public int CommentCount { get; set; }
    public bool SteamPurchase { get; set; }
    public bool ReceivedForFree { get; set; }
    public bool WrittenDuringEarlyAccess { get; set; }
    public bool PrimarilySteamDeck { get; set; }
}