using Core.Contracts;
using Core.Enums;
using Core.Models;
using Core.Models.Export;
using Core.Steam.Models;

namespace Core.Steam.Services.Export;

public class SteamColumnProvider : IStoreColumnProvider
{
    public Store Store { get; } = Store.Steam;

    public Task<List<ExportColumn>> GetAvailableColumnsAsync()
    {
        return Task.FromResult(new List<ExportColumn>
        {
            new ExportColumn
            {
                Key = nameof(ExportColumn.Index), DisplayName = "Index", Description = "Sequential number", IsDefault = true, Index = 1,
                DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(Game.Name), DisplayName = "Game Name", Description = "Name of the game", IsDefault = true,
                Index = 2, DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(Game.Id), DisplayName = "Game ID", Description = "Steam Game ID", IsDefault = true, Index = 3,
                DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.ReviewText), DisplayName = "Review Text", Description = "The actual review content",
                IsDefault = true, Index = 4, DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.RecommendationId), DisplayName = "Recommendation ID",
                Description = "Steam recommendation id", IsDefault = true, Index = 5, DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.IsVotedUp), DisplayName = "Is Recommended",
                Description = "Whether the review recommends the game", IsDefault = true, Index = 6,
                DataType = "boolean"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Language), DisplayName = "Language", Description = "Language of the review",
                IsDefault = true, Index = 7, DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.SteamId), DisplayName = "Author ID",
                Description = "Steam ID of the reviewer", IsDefault = true, Index = 8, DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.GamesOwnedCount), DisplayName = "Games Owned",
                Description = "Number of games owned by gameReview.Author", IsDefault = false, Index = 9, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.ReviewsCount), DisplayName = "Author Reviews Count",
                Description = "Number of reviews by gameReview.Author", IsDefault = false, Index = 10, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.PlayTimeForever), DisplayName = "Play Time (Forever)",
                Description = "Total play time", IsDefault = false, Index = 11, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.PlayTimeLastTwoWeeks), DisplayName = "Play Time (Last 2 Weeks)",
                Description = "Play time in last 2 weeks", IsDefault = false, Index = 12, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.PlayTimeAtReview), DisplayName = "Play Time (At Review)",
                Description = "Play time at moment of review", IsDefault = false, Index = 13, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.DeckPlayTimeAtReview), DisplayName = "Deck Play Time (At Review)",
                Description = "Deck play time at review", IsDefault = false, Index = 14, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.Author.LastPlayed), DisplayName = "Author Last Played",
                Description = "When gameReview.Author last played", IsDefault = false, Index = 15, DataType = "date"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.CreationDate), DisplayName = "Date Created",
                Description = "When the review was created", IsDefault = true, Index = 16, DataType = "date"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.UpdateDate), DisplayName = "Date Updated",
                Description = "When the review was last updated", IsDefault = false, Index = 17, DataType = "date"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.VotesUpCount), DisplayName = "Votes Up",
                Description = "Number of helpful votes", IsDefault = false, Index = 18, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.VotesFunnyCount), DisplayName = "Votes Funny",
                Description = "Number of funny votes", IsDefault = false, Index = 19, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.WeightedVoteScore), DisplayName = "Weighted Vote Score",
                Description = "Weighted score of the review", IsDefault = false, Index = 20, DataType = "number"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.CommentCount), DisplayName = "Comment Count",
                Description = "Number of comments on the review", IsDefault = false, Index = 21, DataType = "number"
            },

            new ExportColumn
            {
                Key = nameof(SteamReview.SteamPurchase), DisplayName = "Steam Purchase",
                Description = "Whether purchased on Steam", IsDefault = false, Index = 22, DataType = "boolean"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.ReceivedForFree), DisplayName = "Received For Free",
                Description = "Whether received for free", IsDefault = false, Index = 23, DataType = "boolean"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.WrittenDuringEarlyAccess), DisplayName = "Early Access Review",
                Description = "Whether written during early access", IsDefault = false, Index = 24, DataType = "boolean"
            },

            new ExportColumn
            {
                Key = nameof(SteamReview.DeveloperResponse), DisplayName = "Developer Response",
                Description = "Developer's response to the review", IsDefault = false, Index = 25, DataType = "string"
            },
            new ExportColumn
            {
                Key = nameof(SteamReview.DeveloperResponseDate), DisplayName = "Developer Response Date",
                Description = "When developer responded", IsDefault = false, Index = 26, DataType = "date"
            },

            new ExportColumn
            {
                Key = nameof(SteamReview.PrimarilySteamDeck), DisplayName = "Steam Deck Review",
                Description = "Whether primarily for Steam Deck", IsDefault = false, Index = 27, DataType = "boolean"
            }
        });
    }

    public Task<object> GetColumnValueAsync(Game game, Review review, string columnKey)
    {
        if (review is not SteamReview gameReview)
        {
            throw new ArgumentException("Steam store requires SteamReview", nameof(review));
        }
        
        return Task.FromResult<object>(
            columnKey switch
            {
                nameof(ExportColumn.Index) => columnKey,
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