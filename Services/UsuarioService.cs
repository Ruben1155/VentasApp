using VentasApp.Models; // Necesario para Usuario y RegisterViewModel
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
    // Servicio para interactuar con los endpoints de Usuario en la API
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl; // URL Base de la API (ej. https://localhost:7255/api/usuario/)
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<UsuarioService> logger)
        {
            _httpClient = httpClientFactory?.CreateClient("VentasApiClient")
                 ?? throw new ArgumentNullException(nameof(httpClientFactory));
            // Ajusta la ruta base para este servicio
            _baseUrl = configuration?["ApiSettings:BaseUrl"]?.TrimEnd('/') + "/api/usuario/"
                 ?? throw new InvalidOperationException("API BaseUrl 'ApiSettings:BaseUrl' not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Obtener todos los usuarios (la API ya no devuelve hash)
        public async Task<IEnumerable<Usuario>> GetAllUsuariosAsync()
        {
            var url = _baseUrl; // Endpoint: GET api/usuario
            _logger.LogInformation("Obteniendo todos los usuarios desde API: {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var usuarios = JsonConvert.DeserializeObject<List<Usuario>>(content);
                _logger.LogInformation("Se obtuvieron {Count} usuarios desde la API.", usuarios?.Count ?? 0);
                return usuarios ?? new List<Usuario>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener usuarios desde API: {Url}", url);
                throw new ApplicationException("No se pudo conectar con la API para obtener usuarios.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetAllUsuariosAsync. URL: {Url}", url);
                throw;
            }
        }

        // Obtener un usuario por ID (la API ya no devuelve hash)
        public async Task<Usuario?> GetUsuarioByIdAsync(int id)
        {
            var url = $"{_baseUrl}{id}"; // Endpoint: GET api/usuario/{id}
            _logger.LogInformation("Obteniendo usuario por ID desde API: {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Usuario ID: {UsuarioId} no encontrado en API (404). URL: {Url}", id, url);
                    return null;
                }
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var usuario = JsonConvert.DeserializeObject<Usuario>(content);
                _logger.LogInformation("Usuario ID: {UsuarioId} obtenido exitosamente desde API.", id);
                return usuario;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al obtener usuario ID {UsuarioId} desde API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para obtener el usuario.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetUsuarioByIdAsync para ID {UsuarioId}. URL: {Url}", id, url);
                throw;
            }
        }

        // Crear un usuario nuevo.
        // Envía el objeto UsuarioModel (que puede incluir Clave si viene del registro).
        // La API determinará si usar esa clave o la por defecto.
        public async Task<Usuario?> CreateUsuarioAsync(Usuario usuario)
        {
            var url = _baseUrl; // Endpoint: POST api/usuario
            _logger.LogInformation("Intentando crear usuario via API: {Url}, Correo: {Correo}, ClaveProporcionada: {HasKey}",
                url, usuario.Correo, !string.IsNullOrWhiteSpace(usuario.Clave));

            // Serializar el objeto UsuarioModel COMPLETO
            var jsonContent = JsonConvert.SerializeObject(usuario);
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
                    _logger.LogWarning("Error al crear usuario (Correo: {Correo}) via API ({StatusCode}): {ErrorMessage}. URL: {Url}",
                                     usuario.Correo, response.StatusCode, errorMessage, url);
                    throw new InvalidOperationException($"Error al crear usuario: {errorMessage}");
                }

                // Éxito (200 OK o 201 Created)
                var createdJson = await response.Content.ReadAsStringAsync();
                // Deserializar al modelo de VentasApp (sin Clave)
                var usuarioCreado = JsonConvert.DeserializeObject<Usuario>(createdJson);
                // Limpiar la clave temporal si existiera en el objeto original (aunque el deserializado no la tendrá)
                if (usuario != null) usuario.Clave = null;
                _logger.LogInformation("Usuario creado exitosamente via API para Correo: {Correo}", usuario?.Correo);
                return usuarioCreado;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al crear usuario (Correo: {Correo}) via API: {Url}", usuario.Correo, url);
                throw new ApplicationException("No se pudo conectar con la API para crear el usuario.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto u otro error de API
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CreateUsuarioAsync (Correo: {Correo}). URL: {Url}", usuario.Correo, url);
                throw;
            }
        }


        // Actualizar un usuario existente (NO envía contraseña)
        public async Task<bool> UpdateUsuarioAsync(int id, Usuario usuario)
        {
            var url = $"{_baseUrl}{id}"; // Endpoint: PUT api/usuario/{id}
            _logger.LogInformation("Intentando actualizar usuario ID: {UsuarioId} via API: {Url}", id, url);

            // Crear objeto anónimo SIN Clave para la actualización estándar
            var usuarioData = new
            {
                usuario.Id,
                usuario.Nombre,
                usuario.Apellido,
                usuario.Correo,
                // Telefono no está en el modelo Usuario de VentasApp, si lo necesitaras, hay que añadirlo
                usuario.Rol
            };

            var jsonContent = JsonConvert.SerializeObject(usuarioData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PutAsync(url, httpContent);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Usuario ID: {UsuarioId} no encontrado en API para actualizar (404). URL: {Url}", id, url);
                    return false;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problem = null; try { problem = JsonConvert.DeserializeObject<ProblemDetails>(error); } catch { }
                    _logger.LogWarning("Conflicto al actualizar usuario ID: {UsuarioId} via API: {StatusCode} - {ErrorResponse}", id, response.StatusCode, problem?.Detail ?? error);
                    throw new InvalidOperationException(problem?.Detail ?? "Conflicto al actualizar el usuario (posiblemente correo duplicado).");
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    _logger.LogInformation("Usuario ID: {UsuarioId} no modificado (304) via API.", id);
                    return false; // Indicar que no hubo cambios
                }
                response.EnsureSuccessStatusCode(); // Espera 204 No Content u otro éxito
                _logger.LogInformation("Usuario ID: {UsuarioId} actualizado exitosamente via API.", id);
                return true; // Éxito
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al actualizar usuario ID: {UsuarioId} via API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para actualizar el usuario.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en UpdateUsuarioAsync para ID: {UsuarioId}. URL: {Url}", id, url);
                throw;
            }
        }

        // Eliminar un usuario
        public async Task<bool> DeleteUsuarioAsync(int id)
        {
            var url = $"{_baseUrl}{id}"; // Endpoint: DELETE api/usuario/{id}
            _logger.LogInformation("Intentando eliminar usuario ID: {UsuarioId} via API: {Url}", id, url);
            try
            {
                var response = await _httpClient.DeleteAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Usuario ID: {UsuarioId} no encontrado en API para eliminar (404). URL: {Url}", id, url);
                    return false;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict) // Conflicto (ej. tiene ventas asociadas)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ProblemDetails? problem = null; try { problem = JsonConvert.DeserializeObject<ProblemDetails>(error); } catch { }
                    _logger.LogWarning("Conflicto al eliminar usuario ID: {UsuarioId} via API: {StatusCode} - {ErrorResponse}", id, response.StatusCode, problem?.Detail ?? error);
                    throw new InvalidOperationException(problem?.Detail ?? "No se pudo eliminar el usuario (puede tener registros asociados).");
                }
                response.EnsureSuccessStatusCode(); // Espera 204 No Content u otro éxito
                _logger.LogInformation("Usuario ID: {UsuarioId} eliminado exitosamente via API.", id);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de HttpRequest al eliminar usuario ID: {UsuarioId} via API: {Url}", id, url);
                throw new ApplicationException("No se pudo conectar con la API para eliminar el usuario.", ex);
            }
            catch (InvalidOperationException) { throw; } // Re-lanzar conflicto
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en DeleteUsuarioAsync para ID: {UsuarioId}. URL: {Url}", id, url);
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
