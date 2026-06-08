using Cuidamed;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

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

await builder.Build().RunAsync();
