using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters; // Necesario para IAsyncActionFilter o ActionFilterAttribute
using Microsoft.AspNetCore.Http; // Necesario para ISession y HttpContext
using System;
using System.Threading.Tasks; // Necesario para Task y async/await

namespace VentasApp.Controllers
{
    // Controlador base abstracto para acciones que requieren rol de Administrador.
    // Los controladores que hereden de aquí tendrán la verificación de rol
    // aplicada automáticamente antes de cada acción.
    public abstract class AdminBaseController : Controller
    {
        // Este método se ejecuta ANTES de cada acción en los controladores que hereden de él.
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Obtener el rol del usuario desde la sesión.
            // Usamos 'context.HttpContext' para acceder al contexto de la solicitud actual.
            string? userRole = context.HttpContext.Session.GetString("UserRole");

            // Verificar si el rol es "Administrador" (ignorando mayúsculas/minúsculas).
            // Si userRole es null (no hay sesión o no se guardó el rol), la condición será falsa.
            if (userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) != true)
            {
                // Si no es administrador:
                // 1. Registrar la denegación de acceso (opcional).
                var logger = context.HttpContext.RequestServices.GetService<ILogger<AdminBaseController>>();
                var requestedPath = context.HttpContext.Request.Path;
                logger?.LogWarning("Acceso denegado a ruta '{Path}' para usuario con rol '{Role}' (o sin sesión).", requestedPath, userRole ?? "N/A");

                // 2. Añadir un mensaje para mostrar al usuario.
                TempData["ErrorMessage"] = "Acceso denegado. Se requiere rol de Administrador.";

                // 3. Redirigir. Puedes elegir a dónde:
                // Opción A: Redirigir a la página de Login (Home/Index).
                // context.Result = RedirectToAction("Index", "Home");

                // Opción B: Mostrar una vista específica de "Acceso Denegado".
                // Busca /Views/Shared/AccessDenied.cshtml o /Views/CurrentController/AccessDenied.cshtml
                context.Result = new ViewResult { ViewName = "AccessDenied" };

                // Importante: No llamar a await next() para detener la ejecución de la acción original.
                await base.OnActionExecutionAsync(context, next); // Llamar al método base si es necesario, pero después de establecer context.Result
                return; // Salir del método
            }

            // Si es administrador:
            // Continuar con la ejecución normal de la acción original.
            // 'next' representa el resto del pipeline de ejecución de la acción.
            await next();
        }
    }
}
