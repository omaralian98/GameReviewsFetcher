namespace Core.Models.Export;

public class ExportColumnSelection
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = false;
    public int Order { get; set; } = 0;
    public string DataType { get; set; } = "string";
}