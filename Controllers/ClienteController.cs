using VentasApp.Models;     // Necesario para Cliente
using VentasApp.Services;    // Necesario para ClienteService
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Para List<>
using System.Threading.Tasks;

namespace VentasApp.Controllers
{
    // Controlador para gestionar las operaciones CRUD de Clientes en la interfaz web
    // TODO: Añadir protección para que solo Admins puedan acceder si es necesario
    // public class ClienteController : AdminBaseController // Heredaría de AdminBaseController si fuera solo para Admins
    public class ClienteController : Controller // Por ahora, hereda de Controller normal
    {
        private readonly ClienteService _clienteService;
        private readonly ILogger<ClienteController> _logger;

        // Inyectar ClienteService y ILogger
        public ClienteController(ClienteService clienteService, ILogger<ClienteController> logger)
        {
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Cliente o /Cliente/Index
        // Muestra la lista de todos los clientes
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Accediendo a la lista de clientes (Cliente/Index).");
            IEnumerable<Cliente> clientes = new List<Cliente>(); // Inicializar lista vacía
            try
            {
                clientes = await _clienteService.GetAllClientesAsync();
                return View(clientes); // Pasa la lista de clientes a la vista
            }
            catch (ApplicationException ex) // Captura errores de conexión con la API
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener clientes.");
                TempData["ErrorMessage"] = ex.Message; // Mostrar mensaje específico
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener la lista de clientes.");
                TempData["ErrorMessage"] = "No se pudo cargar la lista de clientes.";
            }
            return View(clientes); // Devolver vista con lista (posiblemente vacía)
        }

        // GET: /Cliente/Details/5
        // Muestra los detalles de un cliente específico
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Solicitando detalles para Cliente ID: {ClienteId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("Solicitud de detalles con ID de cliente inválido: {ClienteId}", id);
                return BadRequest();
            }

            try
            {
                var cliente = await _clienteService.GetClienteByIdAsync(id);
                if (cliente == null)
                {
                    _logger.LogWarning("Cliente ID: {ClienteId} no encontrado para ver detalles.", id);
                    TempData["ErrorMessage"] = $"Cliente con ID {id} no encontrado.";
                    return NotFound(); // Devuelve vista de No Encontrado o redirige
                    // return RedirectToAction(nameof(Index));
                }
                return View(cliente); // Pasa el cliente encontrado a la vista
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener detalles para Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener detalles para Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = "Error al cargar los detalles del cliente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Cliente/Create
        // Muestra el formulario para crear un nuevo cliente
        public IActionResult Create()
        {
            _logger.LogInformation("Accediendo al formulario de creación de cliente.");
            return View(new Cliente()); // Devuelve la vista con un modelo vacío
        }

        // POST: /Cliente/Create
        // Procesa los datos enviados desde el formulario de creación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            _logger.LogInformation("Intentando crear nuevo cliente con Nombre: {NombreCliente}", cliente?.Nombre);
            if (ModelState.IsValid)
            {
                try
                {
                    var clienteCreado = await _clienteService.CreateClienteAsync(cliente);
                    if (clienteCreado != null)
                    {
                        _logger.LogInformation("Cliente creado exitosamente via API con Nombre: {NombreCliente}, ID: {ClienteId}", cliente.Nombre, clienteCreado.Id);
                        TempData["SuccessMessage"] = "Cliente creado correctamente.";
                        return RedirectToAction(nameof(Index)); // Redirigir a la lista
                    }
                    else
                    {
                        _logger.LogError("La llamada API para crear cliente (Nombre: {NombreCliente}) devolvió null.", cliente.Nombre);
                        ModelState.AddModelError(string.Empty, "Error al crear el cliente en el servidor.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar conflicto (correo duplicado) u otros errores de API
                {
                    _logger.LogWarning(ex, "Conflicto u error de operación al crear cliente con Nombre: {NombreCliente}", cliente.Nombre);
                    ModelState.AddModelError(string.Empty, ex.Message); // Mostrar mensaje específico
                }
                catch (ApplicationException ex) // Error de conexión
                {
                    _logger.LogError(ex, "Error de conexión con la API al crear cliente con Nombre: {NombreCliente}", cliente.Nombre);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex) // Capturar otros errores
                {
                    _logger.LogError(ex, "Error inesperado al crear cliente con Nombre: {NombreCliente}", cliente.Nombre);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al crear el cliente.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para crear cliente inválido. Nombre: {NombreCliente}", cliente?.Nombre);
            }
            // Si hay errores, volver a mostrar el formulario
            return View(cliente);
        }

        // GET: /Cliente/Edit/5
        // Muestra el formulario para editar un cliente existente
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Accediendo al formulario de edición para Cliente ID: {ClienteId}", id);
            if (id <= 0) return BadRequest();

            try
            {
                var cliente = await _clienteService.GetClienteByIdAsync(id);
                if (cliente == null)
                {
                    _logger.LogWarning("Cliente ID: {ClienteId} no encontrado para editar.", id);
                    return NotFound();
                }
                return View(cliente); // Pasa el cliente a la vista de edición
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener datos para editar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para editar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = "Error al cargar los datos del cliente para editar.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Cliente/Edit/5
        // Procesa los datos enviados desde el formulario de edición
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cliente cliente)
        {
            _logger.LogInformation("Intentando actualizar Cliente ID: {ClienteId}", id);
            if (id != cliente.Id)
            {
                _logger.LogWarning("ID de ruta ({RouteId}) no coincide con ID de modelo ({ModelId}) en edición de cliente.", id, cliente.Id);
                return BadRequest("Inconsistencia en el ID del cliente.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var actualizado = await _clienteService.UpdateClienteAsync(id, cliente);
                    if (actualizado)
                    {
                        _logger.LogInformation("Cliente ID: {ClienteId} actualizado exitosamente.", id);
                        TempData["SuccessMessage"] = "Cliente actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // El servicio devolvió false (no encontrado o no modificado)
                        _logger.LogWarning("La llamada API para actualizar Cliente ID: {ClienteId} devolvió false.", id);
                        ModelState.AddModelError(string.Empty, "No se pudo actualizar el cliente. Puede que no exista o no haya cambios.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar conflicto (correo duplicado)
                {
                    _logger.LogWarning(ex, "Conflicto al actualizar Cliente ID: {ClienteId}", id);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ApplicationException ex) // Error de conexión
                {
                    _logger.LogError(ex, "Error de conexión con la API al actualizar Cliente ID: {ClienteId}", id);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado al actualizar Cliente ID: {ClienteId}", id);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el cliente.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para editar Cliente ID: {ClienteId} inválido.", id);
            }
            // Si hay errores, volver a mostrar el formulario
            return View(cliente);
        }

        // GET: /Cliente/Delete/5
        // Muestra la página de confirmación para eliminar un cliente
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Accediendo a la confirmación de eliminación para Cliente ID: {ClienteId}", id);
            if (id <= 0) return BadRequest();

            try
            {
                var cliente = await _clienteService.GetClienteByIdAsync(id);
                if (cliente == null)
                {
                    _logger.LogWarning("Cliente ID: {ClienteId} no encontrado para eliminar.", id);
                    return NotFound();
                }
                return View(cliente); // Pasa el cliente a la vista de confirmación
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener datos para eliminar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para eliminar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = "Error al cargar los datos del cliente para eliminar.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Cliente/Delete/5
        // Ejecuta la eliminación del cliente tras la confirmación
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Confirmada eliminación para Cliente ID: {ClienteId}", id);
            if (id <= 0) return BadRequest();

            try
            {
                var eliminado = await _clienteService.DeleteClienteAsync(id);
                if (eliminado)
                {
                    _logger.LogInformation("Cliente ID: {ClienteId} eliminado exitosamente.", id);
                    TempData["SuccessMessage"] = "Cliente eliminado correctamente.";
                }
                else
                {
                    // No encontrado
                    _logger.LogWarning("La llamada API para eliminar Cliente ID: {ClienteId} devolvió false (no encontrado).", id);
                    TempData["ErrorMessage"] = "No se pudo eliminar el cliente. Es posible que ya no exista.";
                }
            }
            catch (InvalidOperationException ex) // Capturar conflicto (cliente con ventas)
            {
                _logger.LogWarning(ex, "Conflicto al eliminar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = ex.Message; // Mostrar mensaje específico del error
            }
            catch (ApplicationException ex) // Error de conexión
            {
                _logger.LogError(ex, "Error de conexión con la API al eliminar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex) // Capturar otros errores
            {
                _logger.LogError(ex, "Error inesperado al eliminar Cliente ID: {ClienteId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al eliminar el cliente.";
            }

            return RedirectToAction(nameof(Index)); // Siempre redirigir a la lista
        }
    }
}
