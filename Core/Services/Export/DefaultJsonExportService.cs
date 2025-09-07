using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;

namespace Core.Services.Export;

public class DefaultJsonExportService(IStoreColumnProvider columnProvider) : IExportService
{
    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public virtual async Task<ExportationResult> ExportReviewsAsync(IEnumerable<Review> reviews, Game game,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions
        {
            Format = ExportFormat.Json
        };

        if (options.Format != ExportFormat.Json)
        {
            throw new ArgumentException($"This service only supports JSON format, but {options.Format} was requested.");
        }
        
        return await ExportWithSheets(game, options, [(reviews, "rows")]);
    }
    
    protected async Task<ExportationResult> ExportWithSheets(Game game, ExportOptions options,
        params (IEnumerable<Review> reviews, string fieldName)[] data)
    {
        var allColumns = await columnProvider.GetAvailableColumnsAsync();

        var effectiveColumns = options.SelectedColumns
            .IntersectBy(allColumns.Select(sel => sel.Key), x => x.Key)
            .OrderBy(c => c.Order)
            .ToList();

        var root = new Dictionary<string, object?>();

        if (options.IncludeHeaders)
        {
            root["headers"] = effectiveColumns.Select(c => c.DisplayName).ToList();
        }

        int rowIndex = 1;
        foreach (var (reviews, fieldName) in data)
        {
            var rows = new List<Dictionary<string, object?>>();

            foreach (var review in reviews)
            {
                var row = new Dictionary<string, object?>();

                foreach (var column in effectiveColumns)
                {
                    if (column.Key == nameof(ExportColumn.Index))
                    {
                        row[column.DisplayName] = rowIndex++;
                        continue;
                    }
                    
                    var value = await columnProvider.GetColumnValueAsync(game, review, column.Key);
                    row[column.DisplayName] = value;
                }

                rows.Add(row);
            }

            var key = string.IsNullOrWhiteSpace(fieldName) ? "File" : fieldName;
            var finalKey = key;
            var counter = 1;
            while (root.ContainsKey(finalKey))
            {
                finalKey = $"{key}_{counter++}";
            }

            root[finalKey] = rows;
        }

        var json = JsonConvert.SerializeObject(root, _jsonSettings);
        var jsonData = Encoding.UTF8.GetBytes(json);

        var fileName =
            $"""
             {options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}
             {options.Format.GetFileExtension()}
             """;
        
        var exportationFile = new ExportationFile(jsonData, fileName, options.Format.GetMimeType());

        return new ExportationResult([exportationFile], options);
    }
}
