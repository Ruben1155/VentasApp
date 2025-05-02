using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Necesario para ILogger
using System;
// using VentasApp.Services; // Descomenta si necesitas inyectar servicios específicos (ej. para mostrar contadores)

namespace VentasApp.Controllers
{
    // --- Hereda de AdminBaseController ---
    // Esto aplica automáticamente la verificación de rol "Admin" antes de cada acción.
    public class DashboardController : AdminBaseController
    {
        private readonly ILogger<DashboardController> _logger;
        // --- Inyecta otros servicios aquí si los necesitas para el Dashboard ---
        // private readonly UsuarioService _usuarioService;
        // private readonly VentaService _ventaService;

        public DashboardController(
            ILogger<DashboardController> logger
            // , UsuarioService usuarioService // Descomenta si lo inyectas
            // , VentaService ventaService // Descomenta si lo inyectas
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            // _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
        }

        // GET: /Dashboard o /Dashboard/Index
        // Muestra la vista principal del Dashboard
        public IActionResult Index()
        {
            _logger.LogInformation("Admin accediendo al Dashboard principal.");

            // --- Opcional: Obtener datos para mostrar en el Dashboard ---
            // try
            // {
            //     // Ejemplo: Obtener número de usuarios o ventas recientes
            //     // var numeroUsuarios = await _usuarioService.ContarUsuariosAsync();
            //     // var ventasRecientes = await _ventaService.GetUltimasVentasAsync(5);
            //     // ViewBag.NumeroUsuarios = numeroUsuarios;
            //     // ViewBag.VentasRecientes = ventasRecientes;
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Error al obtener datos para el Dashboard.");
            //     // Podrías mostrar un mensaje de error parcial si algunos datos fallan
            //     TempData["ErrorMessage"] = "Error al cargar algunos datos del dashboard.";
            // }
            // --- Fin Opcional ---

            // Devuelve la vista asociada (Views/Dashboard/Index.cshtml)
            return View();
        }

        // --- Puedes añadir más acciones aquí si el Dashboard tiene sub-secciones ---
        // Ejemplo:
        // public IActionResult Estadisticas()
        // {
        //     _logger.LogInformation("Admin accediendo a la sección de estadísticas del Dashboard.");
        //     // ... lógica para estadísticas ...
        //     return View();
        // }
    }
}