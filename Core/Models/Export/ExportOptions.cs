using Core.Enums;

namespace Core.Models.Export;

public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public string? FileName { get; set; }
    public bool IncludeHeaders { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-mm-dd hh:mm:ss";
    public bool ExportGroupsInSeparateFiles { get; set; } = false;
    public ExcelFormatOptions ExcelFormatOptions { get; set; } = new();
    public List<ExportColumnSelection> SelectedColumns { get; set; } = [];
    public List<ExportColumn> GroupedByColumns { get; set; } = [];
}
