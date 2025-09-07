using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;
using Core.Services.Export;
using Core.Steam.Enums;
using Core.Steam.Models;

namespace Core.Steam.Services.Export;

public class SteamDefaultJsonExportService(IStoreColumnProvider columnProvider) : DefaultJsonExportService(columnProvider)
{
    public override async Task<ExportationResult> ExportReviewsAsync(IEnumerable<Review> reviews, Game game,
        ExportOptions? options = null)
    {
        options ??= new SteamExportOptions()
        {
            Format = ExportFormat.Json,
            ReviewSeparationMode = ReviewSeparationMode.Single
        };

        if (options.Format != ExportFormat.Json)
        {
            throw new ArgumentException($"This service only supports JSON format, but {options.Format} was requested.");
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
            var reviewsData = await ExportWithSheets(game, options,
            [
                (positiveReviews, "positive"),
                (negativeReviews, "negative"),
            ]);
            result.ExportationFiles.Add(reviewsData.ExportationFiles[0]);
        }

        return result;
    }
}