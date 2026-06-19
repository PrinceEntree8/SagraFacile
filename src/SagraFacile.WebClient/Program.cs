using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SagraFacile.WebClient;
using SagraFacile.WebClient.Auth;
using SagraFacile.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var appBaseAddress = new Uri(builder.Configuration["services:api:https:0"] ?? builder.HostEnvironment.BaseAddress);

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthorizationMessageHandler>();

builder.Services.AddLocalization();

builder.Services.AddHttpClient<IAuthService, AuthHttpService>(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddHttpClient<IEventService, EventService>(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddHttpClient<IMenuService, MenuService>(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddHttpClient<IReservationService, ReservationService>(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddHttpClient<IUserService, UserService>(client => client.BaseAddress = appBaseAddress)
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    return new HttpClient(handler) { BaseAddress = appBaseAddress };
});

var app = builder.Build();

await app.RunAsync();
