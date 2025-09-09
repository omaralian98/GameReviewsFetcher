using Core.Contracts;
using Core.Models;
using Core.Models.Export;

namespace Core.Services.Export;

public class DefaultColumnProvider : IStoreColumnProvider
{
    public Task<List<ExportColumn>> GetAvailableColumnsAsync()
    {
        return Task.FromResult<List<ExportColumn>>
        (
            [
                new ExportColumn()
                {
                    Key = nameof(ExportColumn.Order),
                    DisplayName = "Index",
                    Description = "Sequential number",
                    IsDefault = true,
                    Order = 1,
                    DataType = "number"
                },
                new ExportColumn()
                {
                    Key = nameof(Game.Id),
                    DisplayName = "Game ID",
                    Description = "Game ID",
                    IsDefault = true,
                    Order = 2,
                    DataType = "string"
                },
                new ExportColumn()
                {
                    Key = nameof(Game.Name),
                    DisplayName = "Game Name",
                    Description = "Game name",
                    IsDefault = true,
                    Order = 3,
                    DataType = "string"
                },
                new ExportColumn
                {
                    Key = nameof(Review.ReviewText), 
                    DisplayName = "Review Text", 
                    Description = "The actual review content",
                    IsDefault = true, 
                    Order = 4, 
                    DataType = "string"
                },
            ]
        );
    }

    public Task<List<ExportColumn>> GetGroupableColumnsAsync() => Task.FromResult<List<ExportColumn>>([]); 

    public Task<object> GetColumnValueAsync(Game game, Review review, ExportColumn column)
    {
        return Task.FromResult<object>(
            column.Key switch
            {
                nameof(ExportColumn.Order) => column.Key,
                nameof(Game.Id) => game.Id,
                nameof(Game.Name) => game.Name,
                nameof(Review.ReviewText) => review.ReviewText,
                _ => string.Empty
            }
        );
    }

}