using VentasApp.Models; // Necesario para LoginViewModel y Usuario (de VentasApp)
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace VentasApp.Services // Asegúrate que el namespace sea correcto
{
    public class HomeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl; // URL Base de la API (ej. https://localhost:7255/api/)
        private readonly ILogger<HomeService> _logger;

        public HomeService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HomeService> logger)
        {
            _httpClient = httpClientFactory?.CreateClient("VentasApiClient") // Usa el cliente nombrado en Program.cs
                 ?? throw new ArgumentNullException(nameof(httpClientFactory));
            // Obtener la URL base de la API desde la configuración
            _baseUrl = configuration?["ApiSettings:BaseUrl"]?.TrimEnd('/') + "/api/" // Asegura que termine con /api/
                 ?? throw new InvalidOperationException("API BaseUrl 'ApiSettings:BaseUrl' not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Método para validar usuario llamando al endpoint POST /api/usuario/validar
        public async Task<Usuario?> ValidarUsuarioAsync(LoginViewModel credenciales)
        {
            var url = $"{_baseUrl}usuario/validar"; // Endpoint específico de validación
            _logger.LogInformation("Llamando a API para validar usuario: {Url}", url);

            // Crear el objeto LoginRequest que espera la API
            var loginRequestData = new
            {
                Correo = credenciales.Correo,
                Clave = credenciales.Clave
            };

            var jsonContent = JsonConvert.SerializeObject(loginRequestData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, httpContent);

                if (response.IsSuccessStatusCode) // 200 OK
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Validación exitosa desde API para correo: {Correo}.", credenciales.Correo);
                    // Deserializar al modelo Usuario de VentasApp (que no tiene ClaveHash)
                    var usuario = JsonConvert.DeserializeObject<Usuario>(jsonResponse);
                    return usuario;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) // 401
                {
                    _logger.LogWarning("Validación fallida desde API para correo: {Correo}. API devolvió Unauthorized (401).", credenciales.Correo);
                    return null; // Credenciales inválidas
                }
                else // Otros errores
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error desde API al validar correo {Correo}. Status: {StatusCode}, Contenido: {ErrorContent}",
                                     credenciales.Correo, response.StatusCode, errorContent);
                    // Lanzar excepción para indicar un problema más grave que solo credenciales inválidas
                    throw new ApplicationException($"Error de la API al validar credenciales ({response.StatusCode}).");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al validar correo {Correo}. URL: {Url}", credenciales.Correo, url);
                throw new ApplicationException("No se pudo conectar con el servicio de autenticación.", ex);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error al deserializar la respuesta JSON de la API para correo {Correo}. URL: {Url}", credenciales.Correo, url);
                throw new ApplicationException("La respuesta del servicio de autenticación no tuvo el formato esperado.", ex);
            }
            // No capturar Exception genérica aquí, dejar que suba si es necesario
        }

        // Podrías añadir aquí el método RegisterUsuarioAsync si prefieres tenerlo separado del UsuarioService,
        // pero tiene más sentido que esté en UsuarioService ya que crea un usuario.
    }
}
