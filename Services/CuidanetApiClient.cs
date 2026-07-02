using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Cuidamed.Models;
// 1. Agregar el namespace para IConfiguration
using Microsoft.Extensions.Configuration;

namespace Cuidamed.Services
{
    public class CuidanetApiClient
    {
        private readonly HttpClient _httpClient;

        // 2. Definir campos para almacenar las rutas dinámicas
        private readonly string _validateUserUrl;
        private readonly string _beneficiarioUrl;
        private readonly string _movimientoConsultaUrl;

        // 3. Modificar el constructor para inyectar IConfiguration
        public CuidanetApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            // Leer la URL base del appsettings (con fallback)
            string baseUrl = configuration["CuidanetServices:BaseUrl"] ?? "https://admin.cuidanet.net/APILIS/api/";
            _httpClient.BaseAddress = new Uri(baseUrl);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // 4. Leer y asignar las rutas de los endpoints
            _validateUserUrl = configuration["CuidanetServices:Endpoints:ValidateUser"] ?? "Auth/validateuser";
            _beneficiarioUrl = configuration["CuidanetServices:Endpoints:Beneficiario"] ?? "Beneficiario";
            _movimientoConsultaUrl = configuration["CuidanetServices:Endpoints:MovimientoConsulta"] ?? "MovimientoServicio/consulta";
        }

        /// <summary>
        /// Endpoint 2: Verifica la validez del token actual.
        /// </summary>
        public async Task<bool> ValidateUserAsync()
        {
            try
            {
                // Usar la variable configurada
                var response = await _httpClient.GetAsync(_validateUserUrl);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ValidateUserResponse>();
                    return result?.Valid ?? false;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Endpoint 3: Consulta y Filtrado de Beneficiarios.
        /// </summary>
        public async Task<List<BeneficiarioDto>> GetBeneficiariosAsync(string? cedula = null, int? beneficiarioId = null, int? titularId = null)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(cedula)) query["cedula"] = cedula;
            if (beneficiarioId.HasValue) query["beneficiarioId"] = beneficiarioId.Value.ToString();
            if (titularId.HasValue) query["titularId"] = titularId.Value.ToString();

            string queryString = query.Count > 0 ? $"?{query}" : string.Empty;

            // Usar la variable configurada
            var response = await _httpClient.GetAsync($"{_beneficiarioUrl}{queryString}");

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException("Acceso denegado: El endpoint requiere parámetros de filtrado explícitos para este usuario (Regla Crítica de Privacidad).");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BeneficiarioDto>>() ?? new List<BeneficiarioDto>();
        }

        /// <summary>
        /// Endpoint 4: Consulta de Ficha de Detalle (Incluye foto en Base64).
        /// </summary>
        public async Task<BeneficiarioDto?> GetBeneficiarioDetalleAsync(int beneficiarioId)
        {
            // Usar la variable configurada para armar la ruta con el ID
            var response = await _httpClient.GetAsync($"{_beneficiarioUrl}/{beneficiarioId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BeneficiarioDto>();
        }

        /// <summary>
        /// Endpoint 5: Consulta de Historial de Movimientos de Servicio.
        /// </summary>
        public async Task<HttpResponseMessage> GetMovimientosServicioAsync(Dictionary<string, string> filtros)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            foreach (var filtro in filtros)
            {
                if (!string.IsNullOrEmpty(filtro.Value))
                {
                    query[filtro.Key] = filtro.Value;
                }
            }

            string queryString = query.Count > 0 ? $"?{query}" : string.Empty;

            // Usar la variable configurada
            var response = await _httpClient.GetAsync($"{_movimientoConsultaUrl}{queryString}");

            response.EnsureSuccessStatusCode();
            return response;
        }

        /// <summary>
        /// POST /api/Imagenes/upload
        /// Sube una imagen o documento al servidor de CuidaNet.
        /// </summary>
        public async Task<UploadImagenResponse?> UploadImagenAsync(
            Stream fileStream,
            string fileName,
            string carpeta,
            int? servicioId = null,
            int? ordenIdOrMedicamentoId = null,
            int? presupuestoCAId = null)
        {
            // El endpoint requiere multipart/form-data
            using var content = new MultipartFormDataContent();

            // 1. Agregar el archivo binario (Debe llamarse exactamente 'file')
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
            content.Add(streamContent, "file", fileName);

            // 2. Agregar parámetros obligatorios del formulario
            content.Add(new StringContent(carpeta), "carpeta");

            // 3. Agregar parámetros condicionales según las reglas de la carpeta
            if (servicioId.HasValue)
                content.Add(new StringContent(servicioId.Value.ToString()), "servicioId");

            if (ordenIdOrMedicamentoId.HasValue)
            {
                // Dependiendo del tipo de carpeta, se mapea al campo correspondiente
                if (carpeta.Equals("Orden", StringComparison.OrdinalIgnoreCase))
                    content.Add(new StringContent(ordenIdOrMedicamentoId.Value.ToString()), "ordenId");
                else if (carpeta.Equals("Medicamento", StringComparison.OrdinalIgnoreCase))
                    content.Add(new StringContent(ordenIdOrMedicamentoId.Value.ToString()), "medicamentoId");
            }

            if (presupuestoCAId.HasValue)
                content.Add(new StringContent(presupuestoCAId.Value.ToString()), "presupuestoCAId");

            var response = await _httpClient.PostAsync("Imagenes/upload", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error {response.StatusCode} en la subida: {errorMsg}");
            }

            return await response.Content.ReadFromJsonAsync<UploadImagenResponse>();
        }

        /// <summary>
        /// GET /api/Imagenes/servicio/{movimientoServicioId}
        /// Lista las imágenes indexadas a un servicio específico.
        /// </summary>
        public async Task<List<UploadImagenResponse>> GetImagenesServicioAsync(int movimientoServicioId, bool soloPendientes = false)
        {
            string url = $"Imagenes/servicio/{movimientoServicioId}";
            if (soloPendientes)
            {
                url += "?soloPendientes=true";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<UploadImagenResponse>>() ?? new List<UploadImagenResponse>();
        }

        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}