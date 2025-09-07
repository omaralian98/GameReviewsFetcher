using ClosedXML.Excel;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;
using Core.Services.Export;
using Core.Steam.Enums;
using Core.Steam.Models;

namespace Core.Steam.Services.Export;

public class SteamDefaultExcelExportService(IStoreColumnProvider columnProvider) : DefaultExcelExportService(columnProvider)
{
    public override async Task<ExportationResult> ExportReviewsAsync(IEnumerable<Review> reviews, Game game,
        ExportOptions? options = null)
    {
        options ??= new SteamExportOptions()
        {
            Format = ExportFormat.Excel,
            ReviewSeparationMode = ReviewSeparationMode.Single
        };

        if (options.Format != ExportFormat.Excel)
        {
            throw new ArgumentException($"This service only supports Excel format, but {options.Format} was requested.");
        }

        if (options is not SteamExportOptions steamExportOptions ||
            steamExportOptions.ReviewSeparationMode == ReviewSeparationMode.Single)
        {
            return await base.ExportReviewsAsync(reviews, game, options);
        }
        

        var reviewsList = reviews.Cast<SteamReview>().ToList();
        var result = new ExportationResult([], options);

        var positiveReviews = reviewsList.Where(review => review is { IsVotedUp: true }).ToList();
        var negativeReviews = reviewsList.Where(review => review is { IsVotedUp: false }).ToList();

        if (steamExportOptions.ReviewSeparationMode == ReviewSeparationMode.Separate)
        {
            if (positiveReviews.Count != 0)
            {
                var positiveData = await base.ExportReviewsAsync(positiveReviews, game, options);

                var fileName = $"""
                                {options.FileName ?? $"{game.Name.Replace(" ", "_")}_Positive_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}
                                {(options.FileName is null ? "" : "_Positive_Reviews")}
                                {options.Format.GetFileExtension()}
                                """;

                var file = positiveData.ExportationFiles[0] with { FileName = fileName };

                result.ExportationFiles.Add(file);
            }

            if (negativeReviews.Count != 0)
            {
                var negativeData = await base.ExportReviewsAsync(negativeReviews, game, options);

                var fileName = $"""
                                {options.FileName ?? $"{game.Name.Replace(" ", "_")}_Negative_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}
                                {(options.FileName is null ? "" : "_Negative_Reviews")}
                                {options.Format.GetFileExtension()}
                                """;

                var file = negativeData.ExportationFiles[0] with { FileName = fileName };

                result.ExportationFiles.Add(file);
            }
        }
        else
        {
            var optionsExcelFormatOptions = options.ExcelFormatOptions ?? new ExcelFormatOptions();

            using var workbook = new XLWorkbook();

            var positiveSheetName = $"{game.Name} Positive Reviews";
            var negativeSheetName = $"{game.Name} Negative Reviews";

            await base.FillWorkSheetAsync(workbook, positiveSheetName, positiveReviews, game, options, optionsExcelFormatOptions);
            await base.FillWorkSheetAsync(workbook, negativeSheetName, negativeReviews, game, options, optionsExcelFormatOptions);

            await using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var fileName =
                $"""
                 {options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}
                 {options.Format.GetFileExtension()}
                 """;
        
            var file = new ExportationFile(bytes, fileName, options.Format.GetMimeType());
            
            result.ExportationFiles.Add(file);
        }

        return result;
    }
}
