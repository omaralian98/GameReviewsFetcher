using Core.Models;
using Core.Models.Export;

namespace Core.Contracts;

public interface IExportService
{
    Task<ExportationResult> ExportReviewsAsync(IEnumerable<Review> reviews, Game game, ExportOptions? options = null);
}
