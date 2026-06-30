using Cuidamed.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Cuidamed.Handlers
{
    public class CuidanetAuthHandler : DelegatingHandler
    {
        private readonly string _usuario;
        private readonly string _password;
        private readonly string _loginUrl;
        private string? _cachedToken;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public CuidanetAuthHandler(string usuario, string password, IConfiguration configuration)
        {
            _usuario = usuario;
            _password = password;

            _loginUrl = configuration["CuidanetServices:loginUrl"]
                        ?? "https://admin.cuidanet.net/APILIS/api/Auth/login";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Inyectar token si ya existe en caché
            string? token = await GetOrRefreshTokenAsync(forceRefresh: false);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 2. Ejecutar la petición original
            var response = await base.SendAsync(request, cancellationToken);

            // 3. Flujo de Manejo de Expiración (401 Unauthorized)
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Forzar la renovación del token
                token = await GetOrRefreshTokenAsync(forceRefresh: true);

                if (!string.IsNullOrEmpty(token))
                {
                    // Re-armar la petición (las peticiones HTTP no se pueden reenviar directamente, hay que clonar o reasignar la cabecera)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Cerrar la respuesta anterior para liberar recursos
                    response.Dispose();

                    // Reintentar de forma automática de manera transparente para el cliente principal
                    response = await base.SendAsync(request, cancellationToken);
                }
            }

            return response;
        }

        private async Task<string?> GetOrRefreshTokenAsync(bool forceRefresh)
        {
            if (!forceRefresh && !string.IsNullOrEmpty(_cachedToken))
            {
                return _cachedToken;
            }

            await _semaphore.WaitAsync();
            try
            {
                // Doble verificación dentro del lock por concurrencia
                if (!forceRefresh && !string.IsNullOrEmpty(_cachedToken))
                {
                    return _cachedToken;
                }

                // Usamos un HttpClient interno aislado exclusivamente para el Login y evitar bucles infinitos
                using var authClient = new HttpClient();
                var payload = new LoginRequest { Usuario = _usuario, Password = _password };

                var response = await authClient.PostAsJsonAsync(_loginUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var loginData = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    _cachedToken = loginData?.Token;
                    return _cachedToken;
                }

                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
