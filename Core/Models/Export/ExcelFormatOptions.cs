using ClosedXML.Excel;

namespace Core.Models.Export;

public class ExcelFormatOptions
{
    public string HeaderBackgroundColor { get; set; } = "#4472C4";
    public string HeaderTextColor { get; set; } = "#FFFFFF";
    public string HeaderFontName { get; set; } = "Arial";
    public double HeaderFontSize { get; set; } = 12;
    public bool HeaderBold { get; set; } = true;
    
    public string DataFontName { get; set; } = "Arial";
    public int DataFontSize { get; set; } = 10;
    public bool DataBold { get; set; } = false;
    
    public string AlternatingRowColor { get; set; } = "#F2F2F2";
    public bool UseAlternatingRows { get; set; } = true;
    
    public bool AutoFitColumns { get; set; } = true;
    public bool FreezeHeaderRow { get; set; } = true;
    public bool AddFilters { get; set; } = false;
    public bool WrapText { get; set; } = false;
    public bool CenterHeaders { get; set; } = false;
    public bool AddBorders { get; set; } = true;
    public int RowHeight { get; set; } = 20;
    public XLBorderStyleValues BorderStyle { get; set; } = XLBorderStyleValues.Thin;
    
    public string SheetName { get; set; } = "Game Reviews";
}
