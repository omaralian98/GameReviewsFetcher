using Core.Enums;

namespace Core.Contracts;

public interface IStoreExportServiceFactory
{
    Task<IExportService> GetService(Store store, ExportFormat format);
    Task<IEnumerable<ExportFormat>> GetSupportedFormats(Store store);
    Task<IExportService> GetService(ExportFormat format);
    Task<IEnumerable<ExportFormat>> GetSupportedFormats();
}