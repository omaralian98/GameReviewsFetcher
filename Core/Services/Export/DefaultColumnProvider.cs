using Core.Contracts;
using Core.Enums;
using Core.Models;
using Core.Models.Export;

namespace Core.Services.Export;

public class DefaultColumnProvider : IStoreColumnProvider
{
    public Store Store { get; } = Store.Default;
    public Task<List<ExportColumn>> GetAvailableColumnsAsync()
    {
        return Task.FromResult<List<ExportColumn>>
        (
            [
                new ExportColumn()
                {
                    Key = nameof(ExportColumn.Index),
                    DisplayName = "Index",
                    Description = "Sequential number",
                    IsDefault = true,
                    Index = 1,
                    DataType = "number"
                },
                new ExportColumn()
                {
                    Key = nameof(Game.Id),
                    DisplayName = "Game ID",
                    Description = "Game ID",
                    IsDefault = true,
                    Index = 2,
                    DataType = "string"
                },
                new ExportColumn()
                {
                    Key = nameof(Game.Name),
                    DisplayName = "Game Name",
                    Description = "Game name",
                    IsDefault = true,
                    Index = 3,
                    DataType = "string"
                },
                new ExportColumn
                {
                    Key = nameof(Review.ReviewText), 
                    DisplayName = "Review Text", 
                    Description = "The actual review content",
                    IsDefault = true, 
                    Index = 4, 
                    DataType = "string"
                },
            ]
        );
    }

    public Task<object> GetColumnValueAsync(Game game, Review review, string columnKey)
    {
        return Task.FromResult<object>(
            columnKey switch
            {
                nameof(ExportColumn.Index) => columnKey,
                nameof(Game.Id) => game.Id,
                nameof(Game.Name) => game.Name,
                nameof(Review.ReviewText) => review.ReviewText,
                _ => string.Empty
            }
        );
    }

}