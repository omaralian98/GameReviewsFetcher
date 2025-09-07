using System.Globalization;
using ClosedXML.Excel;
using Core.Contracts;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Core.Models.Export;

namespace Core.Services.Export;

public class DefaultExcelExportService(IStoreColumnProvider columnProvider) : IExportService
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

        var optionsExcelFormatOptions = options.ExcelFormatOptions;

        using var workbook = new XLWorkbook();

        var sheetName = string.IsNullOrWhiteSpace(optionsExcelFormatOptions.SheetName)
            ? $"{game.Name} Reviews"
            : optionsExcelFormatOptions.SheetName;

        await FillWorkSheetAsync(workbook, sheetName, reviews, game, options, optionsExcelFormatOptions);

        // Prepare byte[] result
        await using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var fileName =
            $"""
             {options.FileName ?? $"{game.Name.Replace(" ", "_")}_Reviews_{DateTime.Now:yyyyMMdd_HHmmss}"}
             {options.Format.GetFileExtension()}
             """;
        
        var file = new ExportationFile(bytes, fileName, options.Format.GetMimeType());

        var files = new List<ExportationFile> { file };
        return new ExportationResult(files, options);
    }

    protected async Task FillWorkSheetAsync(
        XLWorkbook xlWorkbook,
        string sheetName,
        IEnumerable<Review> reviews,
        Game game,
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

        var colCount = reviewsList.Count;

        // Add headers if requested
        if (options.IncludeHeaders)
        {
            for (int columneIndex = 0; columneIndex < colCount; columneIndex++)
            {
                var headerCell = worksheet.Cell(rowIndex, columneIndex + 1);
                headerCell.Value = effectiveColumns[columneIndex].DisplayName;

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

            // If AddFilters is true, set after we have at least header cells.
            if (excelFormatOptions.AddFilters)
            {
                worksheet.Range(rowIndex, 1, rowIndex, colCount).SetAutoFilter();
            }

            rowIndex++;
        }

        // Fill data rows
        var dataStartRow = rowIndex;
        for (int rowInd = 0; rowInd < reviewsList.Count; rowInd++, rowInd++)
        {
            var review = reviewsList[rowInd];

            for (int columnInd = 0; columnInd < colCount; columnInd++)
            {
                var column = effectiveColumns[columnInd];
                object value;
                
                if (column.Key == nameof(ExportColumn.Index))
                {
                    value = columnInd + 1;
                }
                else
                {
                    value = await columnProvider.GetColumnValueAsync(game, review, column.Key);
                }

                var cell = worksheet.Cell(rowInd, columnInd + 1);
    
                cell = PopulateCellWithRespectiveType(cell, value);

                // Data style
                cell.Style.Font.FontName = excelFormatOptions.DataFontName;
                cell.Style.Font.FontSize = excelFormatOptions.DataFontSize;
                cell.Style.Font.Bold = excelFormatOptions.DataBold;
                cell.Style.Alignment.WrapText = excelFormatOptions.WrapText;
                worksheet.Row(rowInd).Height = excelFormatOptions.RowHeight;
            }
        }

        var lastRow = Math.Max(1, rowIndex - 1);
        var lastCol = Math.Max(1, colCount);

        // Apply alternating row color if requested
        if (excelFormatOptions.UseAlternatingRows && reviewsList.Count != 0)
        {
            for (int rowInd = dataStartRow; rowInd <= lastRow; rowInd++)
            {
                // alternate shading (apply to even rows relative to dataStartRow)
                var indexFromStart = rowInd - dataStartRow;
                if (indexFromStart % 2 != 1)
                {
                    continue;
                }

                var rng = worksheet.Range(rowInd, 1, rowInd, lastCol);
                rng.Style.Fill.SetBackgroundColor(XLColor.FromHtml(excelFormatOptions.AlternatingRowColor));
            }
        }

        // Add borders if requested
        if (excelFormatOptions.AddBorders)
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
        if (excelFormatOptions.AutoFitColumns)
        {
            worksheet.Columns(1, lastCol).AdjustToContents();
        }

        // If AddFilters was requested but we didn't set it earlier (no headers), set auto-filter on data
        if (excelFormatOptions.AddFilters && !options.IncludeHeaders)
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

    protected static IXLCell PopulateCellWithRespectiveType(IXLCell cell, object cellValue)
    {
        switch (cellValue)
        {
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.DateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
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
                            out var doubleValue))
                    {
                        cell.Value = doubleValue;
                    }
                    else
                    {
                        cell.Value = cellValue.ToString();
                    }
                }
                else
                {
                    cell.Value = cellValue.ToString();
                }

                break;
            }
        }

        return cell;
    }
}