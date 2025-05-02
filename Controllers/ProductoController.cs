using VentasApp.Models;     // Necesario para Producto
using VentasApp.Services;    // Necesario para ProductoService
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Para List<>
using System.Threading.Tasks;

namespace VentasApp.Controllers
{
    // Controlador para gestionar las operaciones CRUD de Productos en la interfaz web
    // TODO: Proteger si es necesario (ej. solo Admins pueden gestionar productos)
    // public class ProductoController : AdminBaseController
    public class ProductoController : Controller // Por ahora, hereda de Controller normal
    {
        private readonly ProductoService _productoService;
        private readonly ILogger<ProductoController> _logger;

        // Inyectar ProductoService y ILogger
        public ProductoController(ProductoService productoService, ILogger<ProductoController> logger)
        {
            _productoService = productoService ?? throw new ArgumentNullException(nameof(productoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Producto o /Producto/Index
        // Muestra la lista de productos, con filtro opcional por nombre
        public async Task<IActionResult> Index(string? nombreFilter) // Parámetro para filtro
        {
            _logger.LogInformation("Accediendo a la lista de productos con filtro: Nombre='{NombreFilter}'",
                string.IsNullOrEmpty(nombreFilter) ? "N/A" : nombreFilter);

            // Guardar filtro actual para la vista
            ViewData["CurrentNombreFilter"] = nombreFilter;
            IEnumerable<Producto> productos = new List<Producto>();

            try
            {
                // Llamar al servicio pasando el filtro
                productos = await _productoService.GetAllProductosAsync(nombreFilter);
                return View(productos); // Pasa la lista (filtrada o completa) a la vista
            }
            catch (ApplicationException ex) // Error de conexión API
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener productos.");
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener la lista de productos.");
                TempData["ErrorMessage"] = "No se pudo cargar la lista de productos.";
            }
            return View(productos); // Devolver vista con lista (posiblemente vacía)
        }

        // GET: /Producto/Details/5
        // Muestra los detalles de un producto específico
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Solicitando detalles para Producto ID: {ProductoId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var producto = await _productoService.GetProductoByIdAsync(id);
                if (producto == null)
                {
                    _logger.LogWarning("Producto ID: {ProductoId} no encontrado para ver detalles.", id);
                    TempData["ErrorMessage"] = $"Producto con ID {id} no encontrado.";
                    return NotFound();
                }
                return View(producto);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener detalles para Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener detalles para Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = "Error al cargar los detalles del producto.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Producto/Create
        // Muestra el formulario para crear un nuevo producto
        public IActionResult Create()
        {
            _logger.LogInformation("Accediendo al formulario de creación de producto.");
            return View(new Producto());
        }

        // POST: /Producto/Create
        // Procesa los datos enviados desde el formulario de creación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            _logger.LogInformation("Intentando crear nuevo producto con Nombre: {NombreProducto}", producto?.Nombre);
            if (ModelState.IsValid)
            {
                try
                {
                    var productoCreado = await _productoService.CreateProductoAsync(producto);
                    if (productoCreado != null)
                    {
                        _logger.LogInformation("Producto creado exitosamente via API con Nombre: {NombreProducto}, ID: {ProductoId}", producto.Nombre, productoCreado.Id);
                        TempData["SuccessMessage"] = "Producto creado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError("La llamada API para crear producto (Nombre: {NombreProducto}) devolvió null.", producto.Nombre);
                        ModelState.AddModelError(string.Empty, "Error al crear el producto en el servidor.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar errores de API (ej. validación)
                {
                    _logger.LogWarning(ex, "Error de operación al crear producto con Nombre: {NombreProducto}", producto.Nombre);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ApplicationException ex) // Error de conexión
                {
                    _logger.LogError(ex, "Error de conexión con la API al crear producto con Nombre: {NombreProducto}", producto.Nombre);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex) // Capturar otros errores
                {
                    _logger.LogError(ex, "Error inesperado al crear producto con Nombre: {NombreProducto}", producto.Nombre);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al crear el producto.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para crear producto inválido. Nombre: {NombreProducto}", producto?.Nombre);
            }
            // Si hay errores, volver a mostrar el formulario
            return View(producto);
        }

        // GET: /Producto/Edit/5
        // Muestra el formulario para editar un producto existente
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Accediendo al formulario de edición para Producto ID: {ProductoId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var producto = await _productoService.GetProductoByIdAsync(id);
                if (producto == null)
                {
                    _logger.LogWarning("Producto ID: {ProductoId} no encontrado para editar.", id);
                    return NotFound();
                }
                return View(producto);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener datos para editar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para editar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = "Error al cargar los datos del producto para editar.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Producto/Edit/5
        // Procesa los datos enviados desde el formulario de edición
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            _logger.LogInformation("Intentando actualizar Producto ID: {ProductoId}", id);
            if (id != producto.Id)
            {
                _logger.LogWarning("ID de ruta ({RouteId}) no coincide con ID de modelo ({ModelId}) en edición de producto.", id, producto.Id);
                return BadRequest("Inconsistencia en el ID del producto.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var actualizado = await _productoService.UpdateProductoAsync(id, producto);
                    if (actualizado)
                    {
                        _logger.LogInformation("Producto ID: {ProductoId} actualizado exitosamente.", id);
                        TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // No encontrado o no modificado
                        _logger.LogWarning("La llamada API para actualizar Producto ID: {ProductoId} devolvió false.", id);
                        ModelState.AddModelError(string.Empty, "No se pudo actualizar el producto. Puede que no exista o no haya cambios.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar errores de API
                {
                    _logger.LogWarning(ex, "Error de operación al actualizar Producto ID: {ProductoId}", id);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ApplicationException ex) // Error de conexión
                {
                    _logger.LogError(ex, "Error de conexión con la API al actualizar Producto ID: {ProductoId}", id);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado al actualizar Producto ID: {ProductoId}", id);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el producto.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para editar Producto ID: {ProductoId} inválido.", id);
            }
            // Si hay errores, volver a mostrar el formulario
            return View(producto);
        }

        // GET: /Producto/Delete/5
        // Muestra la página de confirmación para eliminar un producto
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Accediendo a la confirmación de eliminación para Producto ID: {ProductoId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var producto = await _productoService.GetProductoByIdAsync(id);
                if (producto == null)
                {
                    _logger.LogWarning("Producto ID: {ProductoId} no encontrado para eliminar.", id);
                    return NotFound();
                }
                return View(producto);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener datos para eliminar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para eliminar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = "Error al cargar los datos del producto para eliminar.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Producto/Delete/5
        // Ejecuta la eliminación del producto tras la confirmación
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Confirmada eliminación para Producto ID: {ProductoId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                var eliminado = await _productoService.DeleteProductoAsync(id);
                if (eliminado)
                {
                    _logger.LogInformation("Producto ID: {ProductoId} eliminado exitosamente.", id);
                    TempData["SuccessMessage"] = "Producto eliminado correctamente.";
                }
                else
                {
                    // No encontrado
                    _logger.LogWarning("La llamada API para eliminar Producto ID: {ProductoId} devolvió false (no encontrado).", id);
                    TempData["ErrorMessage"] = "No se pudo eliminar el producto. Es posible que ya no exista.";
                }
            }
            catch (InvalidOperationException ex) // Capturar conflicto (producto en ventas)
            {
                _logger.LogWarning(ex, "Conflicto al eliminar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = ex.Message; // Mostrar mensaje específico del error
            }
            catch (ApplicationException ex) // Error de conexión
            {
                _logger.LogError(ex, "Error de conexión con la API al eliminar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex) // Capturar otros errores
            {
                _logger.LogError(ex, "Error inesperado al eliminar Producto ID: {ProductoId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al eliminar el producto.";
            }

            return RedirectToAction(nameof(Index)); // Siempre redirigir a la lista
        }
    }
}
