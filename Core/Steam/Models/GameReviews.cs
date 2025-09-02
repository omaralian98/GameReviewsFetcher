namespace Core.Steam.Models;

public class GameReviews
{
    public int Success { get; set; }
    public GameReviewsQuerySummary? Query_Summary { get; set; }
    public List<GameReview> Reviews { get; set; } = [];
    public string Cursor { get; set; } = "*";
}