using Core.Contracts;
using Core.Models;
using Core.Models.Export;
using Core.Steam.Models;

namespace Core.Steam.Services.Export;

public class SteamColumnProvider : IStoreColumnProvider
{
    private readonly List<ExportColumn> _staticColumns =
    [
        new()
        {
            Key = nameof(ExportColumn.Order), DisplayName = "Index", Description = "Sequential number",
            IsDefault = true, Order = 1,
            DataType = "number"
        },
        new()
        {
            Key = nameof(Game.Name), DisplayName = "Game Name", Description = "Name of the game", IsDefault = true,
            Order = 2, DataType = "string"
        },
        new()
        {
            Key = nameof(Game.Id), DisplayName = "Game ID", Description = "Steam Game ID", IsDefault = true, Order = 3,
            DataType = "string"
        },
        new()
        {
            Key = nameof(SteamReview.ReviewText), DisplayName = "Review Text",
            Description = "The actual review content",
            IsDefault = true, Order = 4, DataType = "string"
        },
    ];

    private readonly List<ExportColumn> _userColumns =
    [
        new()
        {
            Key = nameof(SteamReview.Author.SteamId), DisplayName = "Author ID",
            Description = "Steam ID of the reviewer", IsDefault = true, Order = 8, DataType = "string"
        },
        new()
        {
            Key = nameof(SteamReview.Author.GamesOwnedCount), DisplayName = "Games Owned",
            Description = "Number of games owned by gameReview.Author", IsDefault = false, Order = 9,
            DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.Author.ReviewsCount), DisplayName = "Author Reviews Count",
            Description = "Number of reviews by gameReview.Author", IsDefault = false, Order = 10, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.Author.PlayTimeForever), DisplayName = "Play Time (Forever)",
            Description = "Total play time", IsDefault = false, Order = 11, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.Author.PlayTimeLastTwoWeeks), DisplayName = "Play Time (Last 2 Weeks)",
            Description = "Play time in last 2 weeks", IsDefault = false, Order = 12, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.Author.PlayTimeAtReview), DisplayName = "Play Time (At Review)",
            Description = "Play time at moment of review", IsDefault = false, Order = 13, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.Author.DeckPlayTimeAtReview), DisplayName = "Deck Play Time (At Review)",
            Description = "Deck play time at review", IsDefault = false, Order = 14, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.Author.LastPlayed), DisplayName = "Author Last Played",
            Description = "When gameReview.Author last played", IsDefault = false, Order = 15, DataType = "date"
        }
    ];

    private readonly List<ExportColumn> _reviewColumns =
    [
        new()
        {
            Key = nameof(SteamReview.RecommendationId), DisplayName = "Recommendation ID",
            Description = "Steam recommendation id", IsDefault = true, Order = 5, DataType = "string"
        },
        new()
        {
            Key = nameof(SteamReview.IsVotedUp), DisplayName = "Is Recommended",
            Description = "Whether the review recommends the game", IsDefault = true, Order = 6,
            DataType = "boolean"
        },
        new()
        {
            Key = nameof(SteamReview.Language), DisplayName = "Language", Description = "Language of the review",
            IsDefault = true, Order = 7, DataType = "string"
        },
        new()
        {
            Key = nameof(SteamReview.CreationDate), DisplayName = "Date Created",
            Description = "When the review was created", IsDefault = true, Order = 16, DataType = "date"
        },
        new()
        {
            Key = nameof(SteamReview.UpdateDate), DisplayName = "Date Updated",
            Description = "When the review was last updated", IsDefault = false, Order = 17, DataType = "date"
        },
        new()
        {
            Key = nameof(SteamReview.VotesUpCount), DisplayName = "Votes Up",
            Description = "Number of helpful votes", IsDefault = false, Order = 18, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.VotesFunnyCount), DisplayName = "Votes Funny",
            Description = "Number of funny votes", IsDefault = false, Order = 19, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.WeightedVoteScore), DisplayName = "Weighted Vote Score",
            Description = "Weighted score of the review", IsDefault = false, Order = 20, DataType = "number"
        },
        new()
        {
            Key = nameof(SteamReview.CommentCount), DisplayName = "Comment Count",
            Description = "Number of comments on the review", IsDefault = false, Order = 21, DataType = "number"
        },

        new()
        {
            Key = nameof(SteamReview.SteamPurchase), DisplayName = "Steam Purchase",
            Description = "Whether purchased on Steam", IsDefault = false, Order = 22, DataType = "boolean"
        },
        new()
        {
            Key = nameof(SteamReview.ReceivedForFree), DisplayName = "Received For Free",
            Description = "Whether received for free", IsDefault = false, Order = 23, DataType = "boolean"
        },
        new()
        {
            Key = nameof(SteamReview.WrittenDuringEarlyAccess), DisplayName = "Early Access Review",
            Description = "Whether written during early access", IsDefault = false, Order = 24, DataType = "boolean"
        },

        new()
        {
            Key = nameof(SteamReview.DeveloperResponse), DisplayName = "Developer Response",
            Description = "Developer's response to the review", IsDefault = false, Order = 25, DataType = "string"
        },
        new()
        {
            Key = nameof(SteamReview.DeveloperResponseDate), DisplayName = "Developer Response Date",
            Description = "When developer responded", IsDefault = false, Order = 26, DataType = "date"
        },

        new()
        {
            Key = nameof(SteamReview.PrimarilySteamDeck), DisplayName = "Steam Deck Review",
            Description = "Whether primarily for Steam Deck", IsDefault = false, Order = 27, DataType = "boolean"
        }
    ];

    public Task<List<ExportColumn>> GetAvailableColumnsAsync()
    {
        return Task.FromResult<List<ExportColumn>>([.._staticColumns, .._userColumns, .._reviewColumns]);
    }

    public Task<List<ExportColumn>> GetGroupableColumnsAsync()
    {
        return Task.FromResult(_reviewColumns);
    }

    public Task<object> GetColumnValueAsync(Game game, Review review, ExportColumn column)
    {
        if (review is not SteamReview gameReview)
        {
            throw new ArgumentException("Steam store requires SteamReview", nameof(review));
        }

        return Task.FromResult<object>(
            column.Key switch
            {
                nameof(ExportColumn.Order) => column.Key,
                nameof(Game.Id) => game.Id,
                nameof(Game.Name) => game.Name,
                nameof(SteamReview.RecommendationId) => gameReview.RecommendationId,
                nameof(SteamReview.Author.SteamId) => gameReview.Author.SteamId,
                nameof(SteamReview.Author.GamesOwnedCount) => gameReview.Author.GamesOwnedCount,
                nameof(SteamReview.Author.ReviewsCount) => gameReview.Author.ReviewsCount,
                nameof(SteamReview.Author.PlayTimeForever) => gameReview.Author.PlayTimeForever,
                nameof(SteamReview.Author.PlayTimeLastTwoWeeks) => gameReview.Author.PlayTimeLastTwoWeeks,
                nameof(SteamReview.Author.PlayTimeAtReview) => gameReview.Author.PlayTimeAtReview,
                nameof(SteamReview.Author.DeckPlayTimeAtReview) => gameReview.Author.DeckPlayTimeAtReview,
                nameof(SteamReview.Author.LastPlayed) => gameReview.Author.LastPlayed,
                nameof(SteamReview.Language) => gameReview.Language,
                nameof(SteamReview.ReviewText) => gameReview.ReviewText,
                nameof(SteamReview.CreationDate) => gameReview.CreationDate,
                nameof(SteamReview.UpdateDate) => gameReview.UpdateDate,
                nameof(SteamReview.IsVotedUp) => gameReview.IsVotedUp,
                nameof(SteamReview.VotesUpCount) => gameReview.VotesUpCount,
                nameof(SteamReview.VotesFunnyCount) => gameReview.VotesFunnyCount,
                nameof(SteamReview.WeightedVoteScore) => gameReview.WeightedVoteScore,
                nameof(SteamReview.CommentCount) => gameReview.CommentCount,
                nameof(SteamReview.SteamPurchase) => gameReview.SteamPurchase,
                nameof(SteamReview.ReceivedForFree) => gameReview.ReceivedForFree,
                nameof(SteamReview.WrittenDuringEarlyAccess) => gameReview.WrittenDuringEarlyAccess,
                nameof(SteamReview.DeveloperResponse) => gameReview.DeveloperResponse ?? string.Empty,
                nameof(SteamReview.DeveloperResponseDate) => gameReview.DeveloperResponseDate,
                nameof(SteamReview.PrimarilySteamDeck) => gameReview.PrimarilySteamDeck,
                _ => string.Empty
            }
        );
    }
}