using System.Globalization;
using ClosedXML.Excel;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;

namespace Core.Services.Export;

public class ExcelExportService(IStoreColumnProvider columnProvider) : IExportService
{
    public virtual async Task<ExportationResult> ExportReviewsAsync(IEnumerable<Review> reviews, Game game,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions()
        {
            Format = ExportFormat.Excel
        };

        if (options.Format != ExportFormat.Excel)
        {
            throw new ArgumentException($"This service only supports Excel format, but {options.Format} was requested.");
        }

        var reviewsList = reviews.ToList();
        
        var groupableColumns = await columnProvider.GetGroupableColumnsAsync();

        var groupCols = options.GroupedByColumns
            .IntersectBy(groupableColumns.Select(c => c.Key), c => c.Key)
            .OrderBy(c => c.Order)
            .ToList();

        // Not grouped: single worksheet
        if (groupCols.Count == 0)
        {
            using var workbook = new XLWorkbook();

            var sheetName = string.IsNullOrWhiteSpace(options.ExcelFormatOptions.SheetName)
                ? $"{game.Name} Reviews"
                : options.ExcelFormatOptions.SheetName;

            await FillWorkSheetAsync(workbook, sheetName, reviewsList, game, groupCols, options, options.ExcelFormatOptions);

            await using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var fileName = $"{(options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}")}{options.Format.GetFileExtension()}";
            var file = new ExportationFile(bytes, fileName, options.Format.GetMimeType());

            return new ExportationResult([file], options);
        }

        var groups = new Dictionary<string, (List<Review> Reviews, List<(ExportColumn Col, string Value)> Values)>();
        foreach (var review in reviewsList)
        {
            var vals = new List<(ExportColumn Col, string Value)>();
            foreach (var col in groupCols)
            {
                var valObj = await columnProvider.GetColumnValueAsync(game, review, col);
                vals.Add((col, valObj.ToString() ?? string.Empty));
            }

            var key = string.Join("||", vals.Select(v => v.Value));

            if (!groups.TryGetValue(key, out var entry))
            {
                entry = (new List<Review>(), vals);
            }
            entry.Reviews.Add(review);
            groups[key] = entry;
        }


        if (!options.ExportGroupsInSeparateFiles)
        {
            using var workbook = new XLWorkbook();
            
            int sheetIndex = 1;
            foreach (var (_, bucket) in groups)
            {
                var sheetName = BuildGroupSheetName(bucket.Values, sheetIndex);
                await FillWorkSheetAsync(workbook, sheetName, bucket.Reviews, game, groupCols, options, options.ExcelFormatOptions);
                sheetIndex++;
            }

            await using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var fileName = $"{(options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}")}{options.Format.GetFileExtension()}";
            var file = new ExportationFile(bytes, fileName, options.Format.GetMimeType());

            return new ExportationResult([file], options);
        }
        
        var files = new List<ExportationFile>();

        foreach (var (_, bucket) in groups)
        {
            using var workbook = new XLWorkbook();
            var sheetName = string.IsNullOrWhiteSpace(options.ExcelFormatOptions.SheetName)
                ? $"{game.Name} Reviews"
                : options.ExcelFormatOptions.SheetName;

            var childOptions = new ExportOptions
            {
                Format = options.Format,
                FileName = BuildGroupFileName(options, game, bucket.Values),
                IncludeHeaders = options.IncludeHeaders,
                DateFormat = options.DateFormat,
                ExportGroupsInSeparateFiles = false,
                ExcelFormatOptions = options.ExcelFormatOptions,
                SelectedColumns = options.SelectedColumns,
                GroupedByColumns = options.GroupedByColumns
            };

            await FillWorkSheetAsync(workbook, sheetName, bucket.Reviews, game, groupCols, childOptions, options.ExcelFormatOptions);

            await using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var fileName = $"{childOptions.FileName}{childOptions.Format.GetFileExtension()}";
            files.Add(new ExportationFile(bytes, fileName, childOptions.Format.GetMimeType()));
        }

        return new ExportationResult(files, options);
    }
    
    
    protected async Task FillWorkSheetAsync(
        XLWorkbook xlWorkbook,
        string sheetName,
        List<Review> reviews,
        Game game,
        IReadOnlyCollection<ExportColumn> groupColumns,
        ExportOptions options,
        ExcelFormatOptions excelFormatOptions
    )
    {
        var worksheet = xlWorkbook.Worksheets.Add(TrimSheetName(sheetName));
        
        var rowIndex = 1;
        var reviewsList = reviews.ToList();

        var allColumns = await columnProvider.GetAvailableColumnsAsync();

        var effectiveColumns = options.SelectedColumns
            .IntersectBy(allColumns.Select(sel => sel.Key), x => x.Key)
            .OrderBy(c => c.Order)
            .ToList();

        var colCount = effectiveColumns.Count;

        // Add headers if requested
        if (options.IncludeHeaders && colCount > 0)
        {
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                var headerCell = worksheet.Cell(rowIndex, colIndex + 1);
                headerCell.Value = effectiveColumns[colIndex].DisplayName;

                headerCell.Style.Font.FontName = excelFormatOptions.HeaderFontName;
                headerCell.Style.Font.FontSize = excelFormatOptions.HeaderFontSize;
                headerCell.Style.Font.Bold = excelFormatOptions.HeaderBold;
                headerCell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(excelFormatOptions.HeaderBackgroundColor));
                headerCell.Style.Font.FontColor = XLColor.FromHtml(excelFormatOptions.HeaderTextColor);

                if (excelFormatOptions.CenterHeaders)
                {
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                headerCell.Style.Alignment.WrapText = excelFormatOptions.WrapText;
                headerCell.WorksheetRow().Height = excelFormatOptions.RowHeight;
            }

            if (excelFormatOptions.AddFilters)
            {
                worksheet.Range(rowIndex, 1, rowIndex, colCount).SetAutoFilter();
            }

            rowIndex++;
        }

        // Fill data rows
        var dataStartRow = rowIndex;
        for (int r = 0; r < reviewsList.Count; r++)
        {
            var review = reviewsList[r];

            for (int c = 0; c < colCount; c++)
            {
                var column = effectiveColumns[c];
                object value;

                if (string.Equals(column.Key, nameof(ExportColumn.Order), StringComparison.OrdinalIgnoreCase))
                {
                    value = r + 1; // order index
                }
                else
                {
                    value = await columnProvider.GetColumnValueAsync(game, review, column);
                }

                var cell = worksheet.Cell(rowIndex + r, c + 1);
                cell = PopulateCellWithRespectiveType(cell, value, options.DateFormat);

                // Data style
                cell.Style.Font.FontName = excelFormatOptions.DataFontName;
                cell.Style.Font.FontSize = excelFormatOptions.DataFontSize;
                cell.Style.Font.Bold = excelFormatOptions.DataBold;
                cell.Style.Alignment.WrapText = excelFormatOptions.WrapText;
                worksheet.Row(rowIndex + r).Height = excelFormatOptions.RowHeight;
            }
        }

        var lastRow = reviewsList.Count > 0 ? (dataStartRow + reviewsList.Count - 1) : (options.IncludeHeaders ? 1 : dataStartRow);
        var lastCol = Math.Max(1, colCount);

        // Apply alternating row color if requested
        if (excelFormatOptions.UseAlternatingRows && reviewsList.Count > 0 && colCount > 0)
        {
            for (int r = dataStartRow; r <= lastRow; r++)
            {
                var indexFromStart = r - dataStartRow;
                if (indexFromStart % 2 == 1)
                {
                    var rng = worksheet.Range(r, 1, r, lastCol);
                    rng.Style.Fill.SetBackgroundColor(XLColor.FromHtml(excelFormatOptions.AlternatingRowColor));
                }
            }
        }

        // Add borders if requested
        if (excelFormatOptions.AddBorders && colCount > 0)
        {
            var usedRange = worksheet.Range(options.IncludeHeaders ? 1 : dataStartRow, 1, lastRow, lastCol);
            usedRange.Style.Border.OutsideBorder = excelFormatOptions.BorderStyle;
            usedRange.Style.Border.InsideBorder = excelFormatOptions.BorderStyle;
        }

        // Freeze header row if requested
        if (excelFormatOptions.FreezeHeaderRow && options.IncludeHeaders)
        {
            worksheet.SheetView.FreezeRows(1);
        }

        // Auto-fit columns if requested
        if (excelFormatOptions.AutoFitColumns && colCount > 0)
        {
            worksheet.Columns(1, lastCol).AdjustToContents();
        }

        // If AddFilters was requested but we didn't set it earlier (no headers), set auto-filter on data
        if (excelFormatOptions.AddFilters && !options.IncludeHeaders && reviewsList.Count > 0 && colCount > 0)
        {
            worksheet.Range(dataStartRow, 1, lastRow, lastCol).SetAutoFilter();
        }
    }

    protected static string TrimSheetName(string name, string defaultName = "Sheet1")
    {
        // ClosedXML limits sheet name length to 31 characters; trim if needed.
        if (string.IsNullOrWhiteSpace(name))
        {
            return defaultName;
        }

        var t = name;
        if (t.Length > 31)
        {
            t = t[..31];
        }

        return t;
    }

    protected static IXLCell PopulateCellWithRespectiveType(IXLCell cell, object cellValue, string dateFormat)
    {
        switch (cellValue)
        {
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format = dateFormat;
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.DateTime;
                cell.Style.DateFormat.Format = dateFormat;
                break;
            case bool boolean:
                cell.Value = boolean;
                break;
            case byte[] bytes:
                cell.Value = Convert.ToBase64String(bytes);
                break;
            default:
            {
                if (cellValue.IsNumeric())
                {
                    if (double.TryParse(Convert.ToString(cellValue), NumberStyles.Any, CultureInfo.InvariantCulture,
                            out double doubleValue))
                    {
                        cell.Value = doubleValue;
                    }
                    else
                    {
                        cell.Value = cellValue?.ToString();
                    }
                }
                else
                {
                    cell.Value = cellValue?.ToString();
                }

                break;
            }
        }

        return cell;
    }
    

    private static string BuildGroupFileName(ExportOptions options, Game game,
        List<(ExportColumn Col, string Value)> groupValues)
    {
        var baseName = options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}";
        var parts = groupValues.Select(g =>
        {
            var col = SanitizeForFileName(g.Col.DisplayName ?? g.Col.Key);
            var val = SanitizeForFileName(string.IsNullOrWhiteSpace(g.Value) ? "Empty" : g.Value);
            return $"{col}-{val}";
        });
        var suffix = "_by_" + string.Join("__", parts);
        return SanitizeForFileName(baseName + suffix);
    }

    private static string BuildGroupSheetName(List<(ExportColumn Col, string Value)> groupValues, int index)
    {
        var parts = groupValues.Select(g =>
        {
            var col = SanitizeForFileName(g.Col.DisplayName);
            var raw = string.IsNullOrWhiteSpace(g.Value) ? "Empty" : g.Value;
            var val = SanitizeForFileName(raw);
            return $"{col}={val}";
        });

        var label = string.Join("__", parts);

        // Fallback to numbering if label is empty after sanitization or exceeds Excel's 31-char limit
        if (string.IsNullOrWhiteSpace(label) || label.Length > 31)
        {
            return index.ToString(CultureInfo.InvariantCulture);
        }

        return label;
    }

    private static string SanitizeForFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "file";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (char ch in name.Where(ch => !invalid.Contains(ch)))
        {
            sb.Append(ch == ' ' ? '_' : ch);
        }

        var sanitized = sb.ToString();
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}