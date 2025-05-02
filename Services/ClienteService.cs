using VentasApp.Models; // Necesario para Cliente
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

namespace VentasApp.Services
{
    // Servicio para interactuar con los endpoints de Cliente en la API
    public class ClienteService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl; // URL Base de la API (ej. https://localhost:7255/api/)
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ClienteService> logger)
        {
            _httpClient = httpClientFactory?.CreateClient("VentasApiClient")
                 ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _baseUrl = configuration?["ApiSettings:BaseUrl"]?.TrimEnd('/') + "/api/"
                 ?? throw new InvalidOperationException("API BaseUrl 'ApiSettings:BaseUrl' not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Obtener todos los clientes
        public async Task<IEnumerable<Cliente>> GetAllClientesAsync()
        {
            var url = $"{_baseUrl}cliente"; // Endpoint: api/cliente
            _logger.LogInformation("Obteniendo todos los clientes desde API: {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var clientes = JsonConvert.DeserializeObject<List<Cliente>>(content);
                _logger.LogInformation("Se obtuvieron {Count} clientes desde la API.", clientes?.Count ?? 0);
                return clientes ?? new List<Cliente>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener clientes desde API: {Url}", url);
                throw new ApplicationException("No se pudo conectar con la API para obtener clientes.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetAllClientesAsync. URL: {Url}", url);
                throw;
            }
        }

        // Obtener un cliente por ID
        public async Task<Cliente?> GetClienteByIdAsync(int id)
        {
            var url = $"{_baseUrl}cliente/{id}"; // Endpoint: api/cliente/{id}
            _logger.LogInformation("Obteniendo cliente por ID desde API: {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Cliente ID: {ClienteId} no encontrado en API (404). URL: {Url}", id, url);
                    return null;
                }
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var cliente = JsonConvert.DeserializeObject<Cliente>(content);
                _logger.LogInformation("Cliente ID: {ClienteId} obtenido exitosamente desde API.", id);
                return cliente;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener cliente ID {ClienteId} desde API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para obtener el cliente.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetClienteByIdAsync para ID {ClienteId}. URL: {Url}", id, url);
                throw;
            }
        }

        // Crear un nuevo cliente
        public async Task<Cliente?> CreateClienteAsync(Cliente cliente)
        {
            var url = $"{_baseUrl}cliente"; // Endpoint: POST api/cliente
            _logger.LogInformation("Intentando crear cliente via API: {Url}, Nombre: {NombreCliente}", url, cliente.Nombre);

            var jsonContent = JsonConvert.SerializeObject(cliente);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, httpContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problem = null; try { problem = JsonConvert.DeserializeObject<ProblemDetails>(error); } catch { }
                    _logger.LogWarning("Conflicto al crear cliente (Nombre: {NombreCliente}) via API: {StatusCode} - {ErrorResponse}", cliente.Nombre, response.StatusCode, problem?.Detail ?? error);
                    throw new InvalidOperationException(problem?.Detail ?? "Conflicto al crear el cliente (posiblemente correo duplicado).");
                }
                if (!response.IsSuccessStatusCode) // Otros errores (ej. 400 Bad Request)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problemDetails = null; try { problemDetails = JsonConvert.DeserializeObject<ProblemDetails>(errorContent); } catch { }
                    var errorMessage = problemDetails?.Detail ?? errorContent;
                    _logger.LogWarning("Error al crear cliente (Nombre: {NombreCliente}) via API ({StatusCode}): {ErrorMessage}. URL: {Url}", cliente.Nombre, response.StatusCode, errorMessage, url);
                    throw new InvalidOperationException($"Error al crear cliente: {errorMessage}");
                }

                // Éxito (201 Created)
                var createdJson = await response.Content.ReadAsStringAsync();
                var clienteCreado = JsonConvert.DeserializeObject<Cliente>(createdJson);
                _logger.LogInformation("Cliente creado exitosamente via API para Nombre: {NombreCliente}", cliente.Nombre);
                return clienteCreado;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al crear cliente (Nombre: {NombreCliente}) via API: {Url}", cliente.Nombre, url);
                throw new ApplicationException("No se pudo conectar con la API para crear el cliente.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CreateClienteAsync (Nombre: {NombreCliente}). URL: {Url}", cliente.Nombre, url);
                throw;
            }
        }

        // Actualizar un cliente existente
        public async Task<bool> UpdateClienteAsync(int id, Cliente cliente)
        {
            var url = $"{_baseUrl}cliente/{id}"; // Endpoint: PUT api/cliente/{id}
            _logger.LogInformation("Intentando actualizar cliente ID: {ClienteId} via API: {Url}", id, url);

            var jsonContent = JsonConvert.SerializeObject(cliente);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PutAsync(url, httpContent);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Cliente ID: {ClienteId} no encontrado en API para actualizar (404). URL: {Url}", id, url);
                    return false; // Indicar que no se encontró
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problem = null; try { problem = JsonConvert.DeserializeObject<ProblemDetails>(error); } catch { }
                    _logger.LogWarning("Conflicto al actualizar cliente ID: {ClienteId} via API: {StatusCode} - {ErrorResponse}", id, response.StatusCode, problem?.Detail ?? error);
                    throw new InvalidOperationException(problem?.Detail ?? "Conflicto al actualizar el cliente (posiblemente correo duplicado).");
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    _logger.LogInformation("Cliente ID: {ClienteId} no modificado (304) via API.", id);
                    return false; // Indicar que no hubo cambios
                }

                response.EnsureSuccessStatusCode(); // Espera 204 No Content u otro éxito
                _logger.LogInformation("Cliente ID: {ClienteId} actualizado exitosamente via API.", id);
                return true; // Éxito
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al actualizar cliente ID: {ClienteId} via API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para actualizar el cliente.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en UpdateClienteAsync para ID: {ClienteId}. URL: {Url}", id, url);
                throw;
            }
        }

        // Eliminar un cliente
        public async Task<bool> DeleteClienteAsync(int id)
        {
            var url = $"{_baseUrl}cliente/{id}"; // Endpoint: DELETE api/cliente/{id}
            _logger.LogInformation("Intentando eliminar cliente ID: {ClienteId} via API: {Url}", id, url);
            try
            {
                var response = await _httpClient.DeleteAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Cliente ID: {ClienteId} no encontrado en API para eliminar (404). URL: {Url}", id, url);
                    return false;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict) // Conflicto (ej. tiene ventas asociadas)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problem = null; try { problem = JsonConvert.DeserializeObject<ProblemDetails>(error); } catch { }
                    _logger.LogWarning("Conflicto al eliminar cliente ID: {ClienteId} via API: {StatusCode} - {ErrorResponse}", id, response.StatusCode, problem?.Detail ?? error);
                    throw new InvalidOperationException(problem?.Detail ?? "No se pudo eliminar el cliente (puede tener registros asociados).");
                }
                response.EnsureSuccessStatusCode(); // Espera 204 No Content u otro éxito
                _logger.LogInformation("Cliente ID: {ClienteId} eliminado exitosamente via API.", id);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al eliminar cliente ID: {ClienteId} via API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para eliminar el cliente.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en DeleteClienteAsync para ID: {ClienteId}. URL: {Url}", id, url);
                throw;
            }
        }
        // Clase auxiliar interna para deserializar ProblemDetails (si no la tienes ya definida globalmente)
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
