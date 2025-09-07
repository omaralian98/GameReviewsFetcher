namespace Core.Models.Export;

public record ExportationResult(List<ExportationFile> ExportationFiles, ExportOptions? ExportOptions = null);
