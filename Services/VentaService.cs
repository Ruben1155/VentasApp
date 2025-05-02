using VentasApp.Models; // Necesario para Venta, VentaDetalle, VentaInputModel (API), etc.
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Web; // Para HttpUtility
using System.Linq; // Para Any()

namespace VentasApp.Services
{
    // Servicio para interactuar con los endpoints de Venta en la API
    public class VentaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl; // URL Base de la API (ej. https://localhost:7255/api/venta/)
        private readonly ILogger<VentaService> _logger;

        public VentaService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<VentaService> logger)
        {
            _httpClient = httpClientFactory?.CreateClient("VentasApiClient")
                 ?? throw new ArgumentNullException(nameof(httpClientFactory));
            // Ajusta la ruta base para este servicio
            _baseUrl = configuration?["ApiSettings:BaseUrl"]?.TrimEnd('/') + "/api/venta/"
                 ?? throw new InvalidOperationException("API BaseUrl 'ApiSettings:BaseUrl' not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Obtener todas las ventas (cabeceras) con filtros opcionales
        public async Task<IEnumerable<Venta>> GetAllVentasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? idClienteFilter = null)
        {
            var url = _baseUrl; // Endpoint: GET api/venta
            var queryParams = new List<string>();

            _logger.LogInformation("Obteniendo ventas desde API con filtros: Inicio={StartDate}, Fin={EndDate}, Cliente={ClientId}",
                fechaInicio?.ToShortDateString() ?? "N/A",
                fechaFin?.ToShortDateString() ?? "N/A",
                idClienteFilter?.ToString() ?? "N/A");

            // Añadir parámetros de query si tienen valor
            if (fechaInicio.HasValue)
            {
                // Formato ISO 8601 recomendado para fechas en query strings
                queryParams.Add($"fechaInicio={fechaInicio.Value:yyyy-MM-dd}");
            }
            if (fechaFin.HasValue)
            {
                queryParams.Add($"fechaFin={fechaFin.Value:yyyy-MM-dd}");
            }
            if (idClienteFilter.HasValue && idClienteFilter > 0)
            {
                queryParams.Add($"idClienteFilter={idClienteFilter.Value}");
            }

            // Construir la URL final
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            _logger.LogDebug("URL final para obtener ventas: {Url}", url);

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                // Deserializar al modelo Venta de VentasApp
                var ventas = JsonConvert.DeserializeObject<List<Venta>>(content);
                _logger.LogInformation("Se obtuvieron {Count} ventas desde la API aplicando filtros.", ventas?.Count ?? 0);
                return ventas ?? new List<Venta>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener ventas desde API: {Url}", url);
                throw new ApplicationException("No se pudo conectar con la API para obtener las ventas.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetAllVentasAsync. URL: {Url}", url);
                throw;
            }
        }

        // Obtener los detalles de una venta específica
        public async Task<IEnumerable<VentaDetalle>> GetVentaDetallesAsync(int idVenta)
        {
            var url = $"{_baseUrl}{idVenta}/detalles"; // Endpoint: GET api/venta/{idVenta}/detalles
            _logger.LogInformation("Obteniendo detalles para Venta ID: {IdVenta} desde API: {Url}", idVenta, url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Venta ID: {IdVenta} no encontrada en API al buscar detalles (404). URL: {Url}", idVenta, url);
                    return new List<VentaDetalle>(); // Devolver lista vacía si la venta no existe
                }
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var detalles = JsonConvert.DeserializeObject<List<VentaDetalle>>(content);
                _logger.LogInformation("Se obtuvieron {Count} detalles para Venta ID: {IdVenta} desde API.", detalles?.Count ?? 0, idVenta);
                return detalles ?? new List<VentaDetalle>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener detalles para Venta ID {IdVenta} desde API: {Url}", idVenta, url);
                throw new ApplicationException("No se pudo conectar con la API para obtener los detalles de la venta.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetVentaDetallesAsync para Venta ID {IdVenta}. URL: {Url}", idVenta, url);
                throw;
            }
        }


        // Registrar una nueva venta
        // Recibe el VentaCreateViewModel (o un modelo similar) y lo mapea al VentaInputModel que espera la API
        public async Task<Venta?> CreateVentaAsync(VentaCreateViewModel ventaViewModel)
        {
            var url = _baseUrl; // Endpoint: POST api/venta
            _logger.LogInformation("Intentando registrar venta via API: {Url}, ClienteID: {IdCliente}", url, ventaViewModel.IdCliente);

            // Mapear del ViewModel de la UI al InputModel de la API
            var ventaInput = new
            {
                IdCliente = ventaViewModel.IdCliente,
                Detalles = ventaViewModel.Detalles.Select(d => new {
                    d.IdProducto,
                    d.Cantidad
                    // No enviar PrecioUnitario ni Subtotal, la API los calcula/obtiene
                }).ToList()
            };

            var jsonContent = JsonConvert.SerializeObject(ventaInput);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, httpContent);

                // Manejar errores específicos devueltos por la API
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problemDetails = null;
                    try { problemDetails = JsonConvert.DeserializeObject<ProblemDetails>(errorContent); } catch { /* Ignorar */ }
                    var errorMessage = problemDetails?.Detail ?? errorContent;
                    _logger.LogWarning("Error al registrar venta (Cliente: {IdCliente}) via API ({StatusCode}): {ErrorMessage}. URL: {Url}",
                                     ventaViewModel.IdCliente, response.StatusCode, errorMessage, url);
                    throw new InvalidOperationException($"Error al registrar venta: {errorMessage}");
                }

                // Éxito (201 Created)
                var createdJson = await response.Content.ReadAsStringAsync();
                // Deserializar al modelo Venta de VentasApp (que es la cabecera)
                var ventaCreada = JsonConvert.DeserializeObject<Venta>(createdJson);
                _logger.LogInformation("Venta registrada exitosamente via API para ClienteID: {IdCliente}", ventaViewModel.IdCliente);
                return ventaCreada;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al registrar venta (Cliente: {IdCliente}) via API: {Url}", ventaViewModel.IdCliente, url);
                throw new ApplicationException("No se pudo conectar con la API para registrar la venta.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar error de API
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CreateVentaAsync (Cliente: {IdCliente}). URL: {Url}", ventaViewModel.IdCliente, url);
                throw;
            }
        }

        // Podrían añadirse métodos para Actualizar/Eliminar ventas si fuera necesario,
        // llamando a los endpoints correspondientes en la API (que actualmente no existen).

        // Clase auxiliar interna para deserializar ProblemDetails (si no la tienes definida globalmente)
        private class ProblemDetails
        {
            public string? Type { get; set; }
            public string? Title { get; set; }
            public int? Status { get; set; }
            public string? Detail { get; set; }
            public string? Instance { get; set; }
        }
    }
}
