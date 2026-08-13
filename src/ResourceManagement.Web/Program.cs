using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored.LocalStorage;
using ResourceManagement.Web;
using ResourceManagement.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

// MudBlazor
builder.Services.AddMudServices();

// LocalStorage
builder.Services.AddBlazoredLocalStorage();

// API Client Services
builder.Services.AddScoped<IResourceApiService, ResourceApiService>();
builder.Services.AddScoped<IForecastApiService, ForecastApiService>();
builder.Services.AddScoped<IIlcApiService, IlcApiService>();
builder.Services.AddScoped<ILeaveApiService, LeaveApiService>();
builder.Services.AddScoped<IProjectApiService, ProjectApiService>();
builder.Services.AddScoped<ISkillMatrixApiService, SkillMatrixApiService>();
builder.Services.AddScoped<IBandMixApiService, BandMixApiService>();
builder.Services.AddScoped<IImportApiService, ImportApiService>();
builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();

await builder.Build().RunAsync();
