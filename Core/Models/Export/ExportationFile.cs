namespace Core.Models.Export;

public record ExportationFile(byte[] Content, string FileName, string MimeType);
