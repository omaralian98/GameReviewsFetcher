using Core.Enums;
using Core.Models;
using Core.Models.Export;

namespace Core.Contracts;

public interface IStoreColumnProvider
{
    Store Store { get; }
    Task<List<ExportColumn>> GetAvailableColumnsAsync();
    Task<object> GetColumnValueAsync(Game game, Review review, string columnKey);
}
