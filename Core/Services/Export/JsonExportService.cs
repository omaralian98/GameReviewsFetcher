using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;

namespace Core.Services.Export;

public class JsonExportService(IStoreColumnProvider columnProvider) : IExportService
{
    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public async Task<ExportationResult> ExportReviewsAsync(IEnumerable<Review> reviews, Game game,
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

        var reviewsList = reviews.ToList();

        var groupableColumns = await columnProvider.GetGroupableColumnsAsync();

        var groupCols = options.GroupedByColumns
            .IntersectBy(groupableColumns.Select(c => c.Key), c => c.Key)
            .OrderBy(c => c.Order)
            .ToList();

        if (groupCols.Count == 0)
        {
            return await ExportWithGroups(game, options, groupColumns: [], preGroupedData: [(reviewsList, [])]);
        }

        var groups = await BuildGroupsAsync(reviewsList, game, groupCols, options);

        if (options.ExportGroupsInSeparateFiles)
        {
            var files = new List<ExportationFile>();
            foreach (var (_, bucket) in groups)
            {
                var childOptions = new ExportOptions
                {
                    Format = options.Format,
                    FileName = BuildGroupFileName(options, game, bucket.Parts.Select(p => (p.Col, p.Value)).ToList()),
                    IncludeHeaders = options.IncludeHeaders,
                    DateFormat = options.DateFormat,
                    ExportGroupsInSeparateFiles = options.ExportGroupsInSeparateFiles,
                    ExcelFormatOptions = options.ExcelFormatOptions,
                    SelectedColumns = options.SelectedColumns,
                    GroupedByColumns = options.GroupedByColumns
                };

                // Flat export for each group
                var result = await ExportWithGroups(game, childOptions, groupColumns: [],
                    preGroupedData: [(bucket.Reviews, [])]);
                files.Add(result.ExportationFiles[0]);
            }

            return new ExportationResult(files, options);
        }

        var preGrouped = groups.Values
            .Select(v => (v.Reviews, v.Parts.Select(p => p.Token).ToArray()))
            .ToArray();

        return await ExportWithGroups(game, options, groupCols, preGrouped);
    }


    protected async Task<ExportationResult> ExportWithGroups(
        Game game,
        ExportOptions options,
        List<ExportColumn> groupColumns,
        params (List<Review> Reviews, string[] PathTokens)[] preGroupedData)
    {
        var allColumns = await columnProvider.GetAvailableColumnsAsync();

        var effectiveColumns = options.SelectedColumns
            .Where(sel => allColumns.Any(a => string.Equals(a.Key, sel.Key, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.Order)
            .ToList();

        if (options is { ExportGroupsInSeparateFiles: true, GroupedByColumns.Count: > 0 })
        {
            effectiveColumns = effectiveColumns
                .Where(ec =>
                    !options.GroupedByColumns.Any(gc =>
                        string.Equals(gc.Key, ec.Key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (groupColumns.Count == 0)
        {
            var allReviews = preGroupedData[0].Reviews;

            var headers = effectiveColumns.Select(c => c.DisplayName).ToList();
            var rows = await BuildRowsAsync(allReviews, effectiveColumns, groupColumns, game, options);

            var root = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (options.IncludeHeaders)
            {
                root["headers"] = headers;
            }

            root["rows"] = rows;

            return CreateResultFromRoot(root, game, options);
        }

        var headerColumns = effectiveColumns
            .Where(ec => !groupColumns.Any(gc => string.Equals(gc.Key, ec.Key, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var headersForLeaves = headerColumns.Select(c => c.DisplayName).ToList();

        var rootDict = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var leaf in preGroupedData)
        {
            if (leaf.PathTokens.Length != groupColumns.Count)
            {
                throw new ArgumentException(
                    $"PathTokens length ({leaf.PathTokens.Length}) does not match groupColumns count ({groupColumns.Count}).");
            }

            var rows = await BuildRowsAsync(leaf.Reviews, effectiveColumns, groupColumns, game, options);
            InsertRows(rootDict, groupColumns, leaf.PathTokens, rows, headersForLeaves, options.IncludeHeaders);
        }

        return CreateResultFromRoot(rootDict, game, options);
    }


    private async Task<List<Dictionary<string, object?>>> BuildRowsAsync(
        List<Review> reviews,
        IReadOnlyCollection<ExportColumn> effectiveColumns,
        IReadOnlyCollection<ExportColumn> groupColumns,
        Game game,
        ExportOptions options)
    {
        var rows = new List<Dictionary<string, object?>>();

        for (int i = 0; i < reviews.Count; i++)
        {
            var review = reviews[i];
            var row = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var column in effectiveColumns)
            {
                if (groupColumns.FirstOrDefault(x =>
                        string.Equals(x.Key, column.Key, StringComparison.OrdinalIgnoreCase)) is not null)
                {
                    continue;
                }

                if (string.Equals(column.Key, nameof(ExportColumn.Order), StringComparison.OrdinalIgnoreCase))
                {
                    row[column.DisplayName] = i + 1;
                    continue;
                }

                object raw = await columnProvider.GetColumnValueAsync(game, review, column);
                row[column.DisplayName] = raw switch
                {
                    DateTime dt => dt.ToString(options.DateFormat),
                    DateTimeOffset dto => dto.ToString(options.DateFormat),
                    _ => raw
                };
            }

            rows.Add(row);
        }

        return rows;
    }

    private static void InsertRows(
        Dictionary<string, object?> root,
        List<ExportColumn> groupColumns,
        string[] pathTokens,
        List<Dictionary<string, object?>> rows,
        List<string> headers,
        bool includeHeaders)
    {
        for (int level = 0; level < groupColumns.Count; level++)
        {
            var levelName = groupColumns[level].DisplayName;

            if (!root.TryGetValue(levelName, out var colNode) || colNode is not Dictionary<string, object?> colDict)
            {
                colDict = new Dictionary<string, object?>(StringComparer.Ordinal);
                root[levelName] = colDict;
            }

            var valKey = pathTokens[level];

            if (level == groupColumns.Count - 1)
            {
                // leaf: ensure leaf dictionary exists for this value, and add headers + rows there
                if (!colDict.TryGetValue(valKey, out var leafObj) ||
                    leafObj is not Dictionary<string, object?> leafDict)
                {
                    leafDict = new Dictionary<string, object?>(StringComparer.Ordinal);
                    colDict[valKey] = leafDict;
                }

                if (includeHeaders)
                {
                    leafDict["headers"] = new List<string>(headers);
                }

                if (!leafDict.TryGetValue("rows", out var rowsObj) ||
                    rowsObj is not List<Dictionary<string, object?>> rowsList)
                {
                    rowsList = [];
                    leafDict["rows"] = rowsList;
                }

                rowsList.AddRange(rows);
            }
            else
            {
                // intermediate node: descend into next nested dictionary keyed by the value
                if (!colDict.TryGetValue(valKey, out var nextObj) ||
                    nextObj is not Dictionary<string, object?> nextDict)
                {
                    nextDict = new Dictionary<string, object?>(StringComparer.Ordinal);
                    colDict[valKey] = nextDict;
                }

                root = nextDict;
            }
        }
    }

    private static string NormalizeGroupKey(object? value, ExportOptions options)
    {
        return value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            DateTime dt => dt.ToString(options.DateFormat),
            DateTimeOffset dto => dto.ToString(options.DateFormat),
            _ => value.ToString() ?? "null"
        };
    }

    private readonly record struct GroupPart(ExportColumn Col, string Value, string Token);

    private async Task<Dictionary<string, (List<Review> Reviews, List<GroupPart> Parts)>> BuildGroupsAsync(
        List<Review> reviewsList,
        Game game,
        List<ExportColumn> groupCols,
        ExportOptions options)
    {
        char sep = '\u001F';
        var map = new Dictionary<string, (List<Review> Reviews, List<GroupPart> Parts)>(StringComparer.Ordinal);

        foreach (var review in reviewsList)
        {
            var parts = new List<GroupPart>(groupCols.Count);
            foreach (var col in groupCols)
            {
                var raw = await columnProvider.GetColumnValueAsync(game, review, col);
                var value = raw.ToString() ?? string.Empty;
                var token = NormalizeGroupKey(raw, options);
                parts.Add(new GroupPart(col, value, token));
            }

            var key = string.Join(sep, parts.Select(p => p.Token));
            if (!map.TryGetValue(key, out var bucket))
            {
                bucket = (new List<Review>(), parts);
                map[key] = bucket;
            }

            bucket.Reviews.Add(review);
        }

        return map;
    }
    
private ExportationResult CreateResultFromRoot(Dictionary<string, object?> root, Game game, ExportOptions options)
    {
        string json = JsonConvert.SerializeObject(root, _jsonSettings);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        var baseName = options.FileName ?? $"{SanitizeFileName(game.Name)}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}";
        var fileName = baseName + options.Format.GetFileExtension();

        var file = new ExportationFile(bytes, fileName, options.Format.GetMimeType());
        return new ExportationResult([file], options);
    }

    private static string SanitizeFileName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Export";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            sb.Append(invalid.Contains(c) ? '_' : c);
        }

        return sb.ToString().Replace(' ', '_');
    }

    private static string BuildGroupFileName(ExportOptions options, Game game,
        List<(ExportColumn Col, string Value)> groupValues)
    {
        var baseName = options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}";
        var parts = groupValues.Select(g =>
        {
            var col = SanitizeForFileName(g.Col.DisplayName);
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