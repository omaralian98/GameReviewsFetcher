using Core.Enums;

namespace Core.Helpers;

public static class ExtensionMethods
{
    public static string GetFileExtension(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Csv => ".csv",
            ExportFormat.Excel => ".xlsx",
            ExportFormat.Json =>  ".json",
            ExportFormat.Xml => ".xml",
            _ => string.Empty
        };
    }

    public static string GetMimeType(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Csv => "text/csv",
            ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFormat.Json =>  "application/json",
            ExportFormat.Xml => "application/xml",
            _ => string.Empty
        };
    }
    
    public static bool IsNumeric(this object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}