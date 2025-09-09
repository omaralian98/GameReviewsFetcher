using Core.Contracts;
using Core.Enums;
using Core.Steam.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Services.Export;

public class StoreExportServiceFactory(
    IServiceProvider serviceProvider,
    IStoreColumnProviderFactory columnProviderFactory)
    : IStoreExportServiceFactory
{

    public async Task<IExportService> GetService(Store store, ExportFormat format)
    {
        var columnProvider =  await columnProviderFactory.GetProviderAsync(store);

        return (store, format) switch
        {
            (_, ExportFormat.Csv) => ActivatorUtilities.CreateInstance<CsvExportService>(serviceProvider, columnProvider, serviceProvider.GetRequiredService(typeof(ILogger<StoreExportServiceFactory>))),
            (_, ExportFormat.Json) => ActivatorUtilities.CreateInstance<JsonExportService>(serviceProvider, columnProvider),
            (_, ExportFormat.Excel) => ActivatorUtilities.CreateInstance<ExcelExportService>(serviceProvider, columnProvider),

            _ => await GetService(format)
        };
    }
    
    public async Task<IExportService> GetService(ExportFormat format)
    {
        var columnProvider =  await columnProviderFactory.GetProviderAsync();

        return format switch
        {
            ExportFormat.Csv => ActivatorUtilities.CreateInstance<CsvExportService>(serviceProvider, columnProvider),
            ExportFormat.Json => ActivatorUtilities.CreateInstance<JsonExportService>(serviceProvider, columnProvider),
            ExportFormat.Excel => ActivatorUtilities.CreateInstance<ExcelExportService>(serviceProvider, columnProvider),

            _ => throw new NotSupportedException($"Export format {format} is not supported.")
        };
    }
    
    public async Task<IEnumerable<ExportFormat>> GetSupportedFormats(Store store)
    {
        return store switch
        {
            Store.Steam =>
            [
                ExportFormat.Csv,
                ExportFormat.Json,
                ExportFormat.Excel
            ],

            _ => await GetSupportedFormats()
        };
    }

    public Task<IEnumerable<ExportFormat>> GetSupportedFormats()
    {
        return Task.FromResult<IEnumerable<ExportFormat>>
        (
            [
                ExportFormat.Csv,
                ExportFormat.Json,
                ExportFormat.Excel
            ]
        );
    }
}
