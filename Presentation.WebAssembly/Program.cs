using Core.Contracts;
using Core.Services.Export;
using Core.Steam.Interfaces;
using Core.Steam.Services;
using Core.Steam.Services.Export;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Presentation.WebAssembly
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
            builder.Services.AddLogging();
            
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddScoped<IGameStoreReviewsFetcher, SteamStoreReviewsFetcher>();
            builder.Services.AddScoped<ISteamStoreGamesFetcher, SteamGamesPreLoader>();
            
            builder.Services.AddTransient<SteamColumnProvider>();
            builder.Services.AddTransient<DefaultColumnProvider>();
            builder.Services.AddSingleton<IStoreColumnProviderFactory, StoreColumnProviderFactory>();
            
            
            
            builder.Services.AddTransient<CsvExportService>();
            builder.Services.AddTransient<JsonExportService>();
            builder.Services.AddTransient<ExcelExportService>();
            
            builder.Services.AddSingleton<IStoreExportServiceFactory, StoreExportServiceFactory>();
            
            
            await builder.Build().RunAsync();
        }
    }
}
