using VentasApp.Models;     // Necesario para Usuario
using VentasApp.Services;    // Necesario para UsuarioService
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Para List<>
using System.Threading.Tasks;

namespace VentasApp.Controllers
{
    // Controlador para gestionar las operaciones CRUD de Usuarios
    // Hereda de AdminBaseController para asegurar que solo Admins accedan
    public class UsuarioController : AdminBaseController // Asegúrate que AdminBaseController exista
    {
        private readonly UsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;

        // Inyectar UsuarioService y ILogger
        public UsuarioController(UsuarioService usuarioService, ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Usuario o /Usuario/Index
        // Muestra la lista de todos los usuarios
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Admin accediendo a la lista de usuarios (Usuario/Index).");
            IEnumerable<Usuario> usuarios = new List<Usuario>();
            try
            {
                usuarios = await _usuarioService.GetAllUsuariosAsync();
                return View(usuarios);
            }
            catch (ApplicationException ex) // Captura errores de conexión con la API
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener usuarios.");
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener la lista de usuarios.");
                TempData["ErrorMessage"] = "No se pudo cargar la lista de usuarios.";
            }
            return View(usuarios); // Devolver vista con lista (posiblemente vacía)
        }

        // GET: /Usuario/Details/5
        // Muestra los detalles de un usuario específico
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Admin solicitando detalles para Usuario ID: {UsuarioId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario ID: {UsuarioId} no encontrado para ver detalles.", id);
                    TempData["ErrorMessage"] = $"Usuario con ID {id} no encontrado.";
                    return NotFound();
                }
                return View(usuario);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener detalles para Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener detalles para Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = "Error al cargar los detalles del usuario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Usuario/Create
        // Muestra el formulario para crear un nuevo usuario (por Admin)
        public IActionResult Create()
        {
            _logger.LogInformation("Admin accediendo al formulario de creación de usuario.");
            // Pasar un modelo vacío. La contraseña se generará por defecto en la API.
            return View(new Usuario());
        }

        // POST: /Usuario/Create
        // Procesa la creación de usuario por parte del Admin (sin contraseña explícita)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            _logger.LogInformation("Admin intentando crear nuevo usuario con Correo: {Correo}", usuario?.Correo);
            // Remover Clave de la validación, ya que no se envía desde este formulario
            ModelState.Remove(nameof(Usuario.Clave));

            if (ModelState.IsValid)
            {
                try
                {
                    // Llamar al servicio. Como usuario.Clave es null/vacío,
                    // la API generará la contraseña por defecto.
                    var usuarioCreado = await _usuarioService.CreateUsuarioAsync(usuario);
                    if (usuarioCreado != null)
                    {
                        _logger.LogInformation("Usuario creado exitosamente por Admin via API con Correo: {Correo}, ID: {UsuarioId}", usuario.Correo, usuarioCreado.Id);
                        TempData["SuccessMessage"] = $"Usuario '{usuario.Correo}' creado correctamente (con contraseña por defecto).";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError("La llamada API para crear usuario (Correo: {Correo}) devolvió null.", usuario.Correo);
                        ModelState.AddModelError(string.Empty, "Error al crear el usuario en el servidor.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar conflicto (correo duplicado)
                {
                    _logger.LogWarning(ex, "Conflicto al crear usuario con Correo: {Correo}", usuario.Correo);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ApplicationException ex) // Error de conexión
                {
                    _logger.LogError(ex, "Error de conexión con la API al crear usuario con Correo: {Correo}", usuario.Correo);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex) // Capturar otros errores
                {
                    _logger.LogError(ex, "Error inesperado al crear usuario con Correo: {Correo}", usuario.Correo);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al crear el usuario.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para crear usuario (Admin) inválido. Correo: {Correo}", usuario?.Correo);
            }
            // Si hay errores, volver a mostrar el formulario
            return View(usuario);
        }

        // GET: /Usuario/Edit/5
        // Muestra el formulario para editar un usuario existente
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Admin accediendo al formulario de edición para Usuario ID: {UsuarioId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario ID: {UsuarioId} no encontrado para editar.", id);
                    return NotFound();
                }
                return View(usuario); // Pasa el usuario a la vista de edición
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener datos para editar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para editar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = "Error al cargar los datos del usuario para editar.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Usuario/Edit/5
        // Procesa la edición de usuario (NO cambia contraseña)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            _logger.LogInformation("Admin intentando actualizar Usuario ID: {UsuarioId}", id);
            if (id != usuario.Id)
            {
                _logger.LogWarning("ID de ruta ({RouteId}) no coincide con ID de modelo ({ModelId}) en edición de usuario.", id, usuario.Id);
                return BadRequest("Inconsistencia en el ID del usuario.");
            }

            ModelState.Remove(nameof(Usuario.Clave)); // Quitar validación de clave

            if (ModelState.IsValid)
            {
                try
                {
                    // Llamar al servicio para actualizar (sin enviar clave)
                    var actualizado = await _usuarioService.UpdateUsuarioAsync(id, usuario);
                    if (actualizado)
                    {
                        _logger.LogInformation("Usuario ID: {UsuarioId} actualizado exitosamente.", id);
                        TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogWarning("La llamada API para actualizar Usuario ID: {UsuarioId} devolvió false.", id);
                        ModelState.AddModelError(string.Empty, "No se pudo actualizar el usuario. Puede que no exista o no haya cambios.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar conflicto (correo duplicado)
                {
                    _logger.LogWarning(ex, "Conflicto al actualizar Usuario ID: {UsuarioId}", id);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ApplicationException ex) // Error de conexión
                {
                    _logger.LogError(ex, "Error de conexión con la API al actualizar Usuario ID: {UsuarioId}", id);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado al actualizar Usuario ID: {UsuarioId}", id);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el usuario.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para editar Usuario ID: {UsuarioId} inválido.", id);
            }
            // Si hay errores, volver a mostrar el formulario
            return View(usuario);
        }

        // GET: /Usuario/Delete/5
        // Muestra la página de confirmación para eliminar un usuario
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Admin accediendo a la confirmación de eliminación para Usuario ID: {UsuarioId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario ID: {UsuarioId} no encontrado para eliminar.", id);
                    return NotFound();
                }
                return View(usuario); // Pasa el usuario a la vista de confirmación
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener datos para eliminar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para eliminar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = "Error al cargar los datos del usuario para eliminar.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Usuario/Delete/5
        // Ejecuta la eliminación del usuario tras la confirmación
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Admin confirmando eliminación para Usuario ID: {UsuarioId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var eliminado = await _usuarioService.DeleteUsuarioAsync(id);
                if (eliminado)
                {
                    _logger.LogInformation("Usuario ID: {UsuarioId} eliminado exitosamente.", id);
                    TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
                }
                else
                {
                    _logger.LogWarning("La llamada API para eliminar Usuario ID: {UsuarioId} devolvió false (no encontrado).", id);
                    TempData["ErrorMessage"] = "No se pudo eliminar el usuario. Es posible que ya no exista.";
                }
            }
            catch (InvalidOperationException ex) // Capturar conflicto (usuario con ventas)
            {
                _logger.LogWarning(ex, "Conflicto al eliminar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = ex.Message; // Mostrar mensaje específico del error
            }
            catch (ApplicationException ex) // Error de conexión
            {
                _logger.LogError(ex, "Error de conexión con la API al eliminar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex) // Capturar otros errores
            {
                _logger.LogError(ex, "Error inesperado al eliminar Usuario ID: {UsuarioId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al eliminar el usuario.";
            }

            return RedirectToAction(nameof(Index)); // Siempre redirigir a la lista
        }
    }
}
