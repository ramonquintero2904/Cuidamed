using Cuidamed;
using Cuidamed.Handlers;
using Cuidamed.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Registramos nuestro proveedor personalizado como un Singleton/Scoped
builder.Services.AddScoped<CustomAuthStateProvider>();

// 2. Reemplazamos el proveedor por defecto de Blazor por el nuestro
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// 3. Habilitamos el núcleo de autorización de Blazor
builder.Services.AddAuthorizationCore();

// 4. Registrar el handler para un HttpClient específico
builder.Services.AddTransient<CuidanetAuthHandler>(sp =>
{
    var user = builder.Configuration["CuidanetServices:user"] ?? string.Empty;
    var pass = builder.Configuration["CuidanetServices:pass"] ?? string.Empty;

    // Pasamos el usuario, la contraseña y builder.Configuration directamente
    return new CuidanetAuthHandler(user, pass, builder.Configuration);
});

builder.Services.AddHttpClient<CuidanetApiClient>()
    .AddHttpMessageHandler<CuidanetAuthHandler>();

await builder.Build().RunAsync();
