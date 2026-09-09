using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using SagraFacile.WebClient;
using SagraFacile.WebClient.Auth;
using SagraFacile.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var appBaseAddress = new Uri(builder.Configuration["services:api:https:0"] ?? builder.HostEnvironment.BaseAddress);

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddTransient<IReservationRealtimeService, ReservationRealtimeService>();

builder.Services.AddLocalization();

builder.Services.AddRefitGeneratedClient<IAuthService>()
    .ConfigureHttpClient(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddRefitGeneratedClient<IEventService>()
    .ConfigureHttpClient(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddRefitGeneratedClient<IMenuService>()
    .ConfigureHttpClient(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddRefitGeneratedClient<IReservationService>()
    .ConfigureHttpClient(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddRefitGeneratedClient<IUserService>()
    .ConfigureHttpClient(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    return new HttpClient(handler) { BaseAddress = appBaseAddress };
});

builder.Services.AddBlazorBootstrap();

var app = builder.Build();

await app.RunAsync();
