namespace Core.Models.Export;

public class ExportColumn
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public int Index { get; set; } = 0;
    public string DataType { get; set; } = "string";
}
