using Core.Models;
using Core.Models.Export;

namespace Core.Contracts;

public interface IStoreColumnProvider
{
    Task<List<ExportColumn>> GetAvailableColumnsAsync();
    Task<List<ExportColumn>> GetGroupableColumnsAsync();
    Task<object> GetColumnValueAsync(Game game, Review review, ExportColumn column);
}
