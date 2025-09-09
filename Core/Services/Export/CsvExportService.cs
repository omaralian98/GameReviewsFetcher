using System.Text;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Core.Services.Export;

public class CsvExportService(IStoreColumnProvider columnProvider, ILogger<StoreExportServiceFactory> logger)
    : IExportService
{
    public virtual async Task<ExportationResult> ExportReviewsAsync(
        IEnumerable<Review> reviews,
        Game game,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions
        {
            Format = ExportFormat.Csv
        };

        if (options.Format != ExportFormat.Csv)
        {
            throw new ArgumentException($"This service only supports CSV format, but {options.Format} was requested.");
        }

        var groupableColumns = await columnProvider.GetGroupableColumnsAsync();

        var groupCols = options.GroupedByColumns
            .IntersectBy(groupableColumns.Select(c => c.Key), c => c.Key)
            .OrderBy(c => c.Order)
            .ToList();

        if (!options.ExportGroupsInSeparateFiles || groupCols.Count == 0)
        {
            var reviewsList = reviews.ToList();
            return await ExportAsync(reviewsList, game, groupCols, options);
        }

        List<ExportationFile> files = [];

        var groups = new Dictionary<string, (List<Review> Reviews, List<(ExportColumn Col, string Value)> Values)>();
        foreach (var review in reviews)
        {
            var vals = new List<(ExportColumn Col, string Value)>();
            foreach (var col in groupCols)
            {
                var valObj = await columnProvider.GetColumnValueAsync(game, review, col);
                vals.Add((col, valObj?.ToString() ?? string.Empty));
            }

            var key = string.Join("||", vals.Select(v => v.Value));

            if (!groups.TryGetValue(key, out var entry))
            {
                entry = (new List<Review>(), vals);
            }

            entry.Reviews.Add(review);
            groups[key] = entry;
        }

        foreach (var kvp in groups)
        {
            var (groupReviews, groupValues) = kvp.Value;

            var childOptions = new ExportOptions
            {
                Format = options.Format,
                FileName = BuildGroupFileName(options, game, groupValues),
                IncludeHeaders = options.IncludeHeaders,
                DateFormat = options.DateFormat,
                ExportGroupsInSeparateFiles = options.ExportGroupsInSeparateFiles,
                ExcelFormatOptions = options.ExcelFormatOptions,
                SelectedColumns = options.SelectedColumns,
                GroupedByColumns = options.GroupedByColumns
            };

            var result = await ExportAsync(groupReviews, game, groupCols, childOptions);
            files.Add(result.ExportationFiles[0]);
        }

        return new ExportationResult(files, options);
    }

    protected async Task<ExportationResult> ExportAsync(
        List<Review> reviews,
        Game game,
        List<ExportColumn> groupColumns,
        ExportOptions options)
    {
        var csv = new StringBuilder();

        var allColumns = await columnProvider.GetAvailableColumnsAsync();

        var effectiveColumns = options.SelectedColumns
            .IntersectBy(allColumns.Select(sel => sel.Key), x => x.Key)
            .OrderBy(c => c.Order)
            .ToList();


        if (options.IncludeHeaders)
        {
            csv.AppendLine(string.Join(",",
                effectiveColumns
                    .Where(k => !options.ExportGroupsInSeparateFiles || (options.ExportGroupsInSeparateFiles &&
                                groupColumns.FirstOrDefault(x =>
                                    string.Equals(k.Key, x.Key, StringComparison.OrdinalIgnoreCase)) is null))
                    .Select(c => EscapeCsv(c.DisplayName))));
        }

        for (int i = 0; i < reviews.Count; i++)
        {
            var review = reviews[i];
            var rowValues = new List<string>();

            foreach (var column in effectiveColumns)
            {
                if (options.ExportGroupsInSeparateFiles && groupColumns.FirstOrDefault(x =>
                        string.Equals(x.Key, column.Key, StringComparison.OrdinalIgnoreCase)) is not null)
                {
                    continue;
                }

                if (string.Equals(column.Key, nameof(ExportColumn.Order), StringComparison.OrdinalIgnoreCase))
                {
                    rowValues.Add((i + 1).ToString());
                    continue;
                }

                var value = await columnProvider.GetColumnValueAsync(game, review, column);
                rowValues.Add(EscapeCsv(value.ToString() ?? string.Empty));
            }

            csv.AppendLine(string.Join(",", rowValues));
        }

        var data = Encoding.UTF8.GetBytes(csv.ToString());

        var fileName = $"{options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}{options.Format.GetFileExtension()}";

        var file = new ExportationFile(data, fileName, options.Format.GetMimeType());

        return new ExportationResult([file], options);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n');
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        return needsQuotes ? $"\"{value}\"" : value;
    }

    private static string BuildGroupFileName(ExportOptions options, Game game,
        List<(ExportColumn Col, string Value)> groupValues)
    {
        var baseName = options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}";
        // Suffix like _by_{Col1}-{Val1}__{Col2}-{Val2}
        var parts = groupValues.Select(g =>
        {
            var col = SanitizeForFileName(g.Col.DisplayName ?? g.Col.Key);
            var val = SanitizeForFileName(string.IsNullOrWhiteSpace(g.Value) ? "Empty" : g.Value);
            return $"{col}-{val}";
        });
        var suffix = "_by_" + string.Join("__", parts);
        return SanitizeForFileName(baseName + suffix);
    }

    private static string SanitizeForFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "file";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (char ch in name.Where(ch => !invalid.Contains(ch)))
        {
            sb.Append(ch == ' ' ? '_' : ch);
        }

        var sanitized = sb.ToString();
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}