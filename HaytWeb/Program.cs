using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using HaytWeb.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HaytWeb;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// [Phase 1] تنظیم آدرس سرور از appsettings
var serverUrl = builder.Configuration["ApiSettings:ServerBaseUrl"] ?? "http://localhost:5088";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(serverUrl) });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
await builder.Build().RunAsync();







