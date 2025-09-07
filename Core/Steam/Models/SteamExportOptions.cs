using Core.Models.Export;
using Core.Steam.Enums;

namespace Core.Steam.Models;

public class SteamExportOptions : ExportOptions
{
    public ReviewSeparationMode ReviewSeparationMode { get; set; } = ReviewSeparationMode.Single;
}