using VentasApp.Models; // Necesario para Producto
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
    // Servicio para interactuar con los endpoints de Producto en la API
    public class ProductoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl; // URL Base de la API (ej. https://localhost:7255/api/producto/)
        private readonly ILogger<ProductoService> _logger;

        public ProductoService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ProductoService> logger)
        {
            _httpClient = httpClientFactory?.CreateClient("VentasApiClient")
                 ?? throw new ArgumentNullException(nameof(httpClientFactory));
            // Ajusta la ruta base para este servicio
            _baseUrl = configuration?["ApiSettings:BaseUrl"]?.TrimEnd('/') + "/api/producto/"
                 ?? throw new InvalidOperationException("API BaseUrl 'ApiSettings:BaseUrl' not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Obtener todos los productos (con filtro opcional por nombre)
        public async Task<IEnumerable<Producto>> GetAllProductosAsync(string? nombreFilter = null)
        {
            var url = _baseUrl; // La URL base ya incluye /api/producto/
            var queryParams = new List<string>();

            _logger.LogInformation("Obteniendo productos desde API con filtro: Nombre='{NombreFilter}'",
                string.IsNullOrEmpty(nombreFilter) ? "N/A" : nombreFilter);

            // Añadir parámetro de query si tiene valor
            if (!string.IsNullOrWhiteSpace(nombreFilter))
            {
                queryParams.Add($"nombreFilter={HttpUtility.UrlEncode(nombreFilter)}");
            }

            // Construir la URL final con los query parameters si existen
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }

            _logger.LogDebug("URL final para obtener productos: {Url}", url);

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var productos = JsonConvert.DeserializeObject<List<Producto>>(content);
                _logger.LogInformation("Se obtuvieron {Count} productos desde la API aplicando filtros.", productos?.Count ?? 0);
                return productos ?? new List<Producto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener productos desde API: {Url}", url);
                throw new ApplicationException("No se pudo conectar con la API para obtener productos.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetAllProductosAsync. URL: {Url}", url);
                throw;
            }
        }

        // Obtener un producto por ID
        public async Task<Producto?> GetProductoByIdAsync(int id)
        {
            var url = $"{_baseUrl}{id}"; // Endpoint: api/producto/{id}
            _logger.LogInformation("Obteniendo producto por ID desde API: {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Producto ID: {ProductoId} no encontrado en API (404). URL: {Url}", id, url);
                    return null;
                }
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var producto = JsonConvert.DeserializeObject<Producto>(content);
                _logger.LogInformation("Producto ID: {ProductoId} obtenido exitosamente desde API.", id);
                return producto;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener producto ID {ProductoId} desde API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para obtener el producto.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetProductoByIdAsync para ID {ProductoId}. URL: {Url}", id, url);
                throw;
            }
        }

        // Crear un nuevo producto
        public async Task<Producto?> CreateProductoAsync(Producto producto)
        {
            var url = _baseUrl; // Endpoint: POST api/producto
            _logger.LogInformation("Intentando crear producto via API: {Url}, Nombre: {NombreProducto}", url, producto.Nombre);

            var jsonContent = JsonConvert.SerializeObject(producto);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, httpContent);

                // Manejar errores específicos devueltos por la API
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problemDetails = null; try { problemDetails = JsonConvert.DeserializeObject<ProblemDetails>(errorContent); } catch { /* Ignorar */ }
                    var errorMessage = problemDetails?.Detail ?? errorContent;
                    _logger.LogWarning("Error al crear producto (Nombre: {NombreProducto}) via API ({StatusCode}): {ErrorMessage}. URL: {Url}",
                                     producto.Nombre, response.StatusCode, errorMessage, url);
                    throw new InvalidOperationException($"Error al crear producto: {errorMessage}");
                }

                // Éxito (201 Created)
                var createdJson = await response.Content.ReadAsStringAsync();
                var productoCreado = JsonConvert.DeserializeObject<Producto>(createdJson);
                _logger.LogInformation("Producto creado exitosamente via API para Nombre: {NombreProducto}", producto.Nombre);
                return productoCreado;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al crear producto (Nombre: {NombreProducto}) via API: {Url}", producto.Nombre, url);
                throw new ApplicationException("No se pudo conectar con la API para crear el producto.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar error de API
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CreateProductoAsync (Nombre: {NombreProducto}). URL: {Url}", producto.Nombre, url);
                throw;
            }
        }

        // Actualizar un producto existente
        public async Task<bool> UpdateProductoAsync(int id, Producto producto)
        {
            var url = $"{_baseUrl}{id}"; // Endpoint: PUT api/producto/{id}
            _logger.LogInformation("Intentando actualizar producto ID: {ProductoId} via API: {Url}", id, url);

            var jsonContent = JsonConvert.SerializeObject(producto);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PutAsync(url, httpContent);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Producto ID: {ProductoId} no encontrado en API para actualizar (404). URL: {Url}", id, url);
                    return false;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    _logger.LogInformation("Producto ID: {ProductoId} no modificado (304) via API.", id);
                    return false; // Indicar que no hubo cambios
                }

                if (!response.IsSuccessStatusCode) // Otros errores
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problemDetails = null; try { problemDetails = JsonConvert.DeserializeObject<ProblemDetails>(errorContent); } catch { }
                    var errorMessage = problemDetails?.Detail ?? errorContent;
                    _logger.LogWarning("Error al actualizar producto ID: {ProductoId} via API ({StatusCode}): {ErrorMessage}. URL: {Url}", id, response.StatusCode, errorMessage, url);
                    throw new InvalidOperationException($"Error al actualizar producto: {errorMessage}");
                }

                // Éxito (normalmente 204 No Content)
                _logger.LogInformation("Producto ID: {ProductoId} actualizado exitosamente via API.", id);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al actualizar producto ID: {ProductoId} via API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para actualizar el producto.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar error de API
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en UpdateProductoAsync para ID: {ProductoId}. URL: {Url}", id, url);
                throw;
            }
        }

        // Eliminar un producto
        public async Task<bool> DeleteProductoAsync(int id)
        {
            var url = $"{_baseUrl}{id}"; // Endpoint: DELETE api/producto/{id}
            _logger.LogInformation("Intentando eliminar producto ID: {ProductoId} via API: {Url}", id, url);
            try
            {
                var response = await _httpClient.DeleteAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Producto ID: {ProductoId} no encontrado en API para eliminar (404). URL: {Url}", id, url);
                    return false;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict) // Conflicto (ej. producto en ventas)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problem = null; try { problem = JsonConvert.DeserializeObject<ProblemDetails>(error); } catch { }
                    _logger.LogWarning("Conflicto al eliminar producto ID: {ProductoId} via API: {StatusCode} - {ErrorResponse}", id, response.StatusCode, problem?.Detail ?? error);
                    throw new InvalidOperationException(problem?.Detail ?? "No se pudo eliminar el producto (puede estar asociado a ventas).");
                }
                response.EnsureSuccessStatusCode(); // Espera 204 No Content u otro éxito
                _logger.LogInformation("Producto ID: {ProductoId} eliminado exitosamente via API.", id);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al eliminar producto ID: {ProductoId} via API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para eliminar el producto.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en DeleteProductoAsync para ID: {ProductoId}. URL: {Url}", id, url);
                throw;
            }
        }

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
