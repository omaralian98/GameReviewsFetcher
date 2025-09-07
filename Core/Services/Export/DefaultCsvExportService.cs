using System.Text;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Core.Services.Export;

public class DefaultCsvExportService(IStoreColumnProvider columnProvider, ILogger<StoreExportServiceFactory> logger) : IExportService
{
    public virtual async Task<ExportationResult> ExportReviewsAsync(
        IEnumerable<Review> reviews,
        Game game,
        ExportOptions? options = null)
    {
        StringBuilder stringBuilder = new("Columns: ");
        foreach (var selectedColumn in options?.SelectedColumns ?? [])
        {
            stringBuilder.AppendLine($"{selectedColumn.Order}, {selectedColumn.Key}, {selectedColumn.DisplayName}, {selectedColumn.DataType}");
        }
        
        logger.LogInformation(stringBuilder.ToString());
        options ??= new ExportOptions
        {
            Format = ExportFormat.Csv
        };

        if (options.Format != ExportFormat.Csv)
        {
            throw new ArgumentException(
                $"This service only supports CSV format, but {options.Format} was requested.");
        }

        var csv = new StringBuilder();

        var allColumns = await columnProvider.GetAvailableColumnsAsync();
        
         var effectiveColumns = options.SelectedColumns
             .IntersectBy(allColumns.Select(sel => sel.Key), x => x.Key)
             .OrderBy(c => c.Order)
             .ToList();
        
         
         if (options.IncludeHeaders)
         {
            csv.AppendLine(string.Join(",", effectiveColumns.Select(c => EscapeCsv(c.DisplayName))));
         }

        int rowIndex = 1;
        foreach (var review in reviews)
        {
            var rowValues = new List<string>();

            foreach (var column in effectiveColumns)
            {
                if (column.Key == nameof(ExportColumn.Index))
                {
                    rowValues.Add(rowIndex++.ToString());
                    continue;
                }
                
                var value = await columnProvider.GetColumnValueAsync(game, review, column.Key);
                rowValues.Add(EscapeCsv(value.ToString() ?? string.Empty));
            }

            csv.AppendLine(string.Join(",", rowValues));
        }

        var data = Encoding.UTF8.GetBytes(csv.ToString());

        var fileName = $"""
                        {options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}
                        {options.Format.GetFileExtension()}
                        """;

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
}
