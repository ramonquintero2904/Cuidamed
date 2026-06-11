using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private const string StorageKey = "user_session";

    // Inyectamos IJSRuntime a través del constructor
    public CustomAuthStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // Este método se ejecuta AUTOMÁTICAMENTE cada vez que la app arranca o se recarga
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Buscamos si existe la sesión guardada en el navegador
         /*   var cedula = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);

            if (string.IsNullOrWhiteSpace(cedula))
            {*/
                return new AuthenticationState(_anonymous);
          /*  }

            // Si existe, creamos la identidad directamente sin pedir credenciales
            var user = CreateClaimsPrincipal(cedula);
            return new AuthenticationState(user);*/
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    // Modificamos este método para que sea asíncrono y guarde en LocalStorage
    public async Task MarkUserAsAuthenticated(string cedula)
    {
        // Guardamos la cédula en el LocalStorage del navegador
        //await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, cedula);

        var user = CreateClaimsPrincipal(cedula);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    // Modificamos el cierre de sesión para limpiar el LocalStorage
    public async Task MarkUserAsLoggedOut()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    // Función auxiliar para evitar repetir código de Claims
    private ClaimsPrincipal CreateClaimsPrincipal(string cedula)
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, cedula),
            new Claim(ClaimTypes.Role, "Usuario")
        };
        var identity = new ClaimsIdentity(claims, "SMSAuth");
        return new ClaimsPrincipal(identity);
    }
}