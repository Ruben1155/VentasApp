using VentasApp.Models;
using VentasApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
// --- Añadir using para Sesión ---
using Microsoft.AspNetCore.Http;

namespace VentasApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HomeService _homeService; // Servicio para Login
        private readonly UsuarioService _usuarioService; // Servicio para Registro

        // Constructor con todos los servicios inyectados
        public HomeController(
            ILogger<HomeController> logger,
            HomeService homeService,
            UsuarioService usuarioService) // Asegúrate que UsuarioService esté registrado en Program.cs
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _homeService = homeService ?? throw new ArgumentNullException(nameof(homeService));
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
        }

        // GET: /Home/Index o /
        // Muestra la página de Login
        [HttpGet]
        public IActionResult Index()
        {
            // Si ya está logueado, redirigir a la página principal adecuada
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                // Redirigir a Dashboard si es Admin, o a Ventas si es Vendedor
                string? userRole = HttpContext.Session.GetString("UserRole");
                if (userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    return RedirectToAction("Index", "Venta"); // Vendedores van a Ventas
                }
            }
            // Pasar mensaje de éxito si viene desde el registro
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(new LoginViewModel()); // Mostrar vista de Login
        }

        // POST: /Home/Login
        // Procesa el inicio de sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            _logger.LogInformation("Intento de login para {Correo}", loginViewModel.Correo);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modelo de login inválido.");
                // Añadir mensaje de error genérico si el modelo es inválido
                ModelState.AddModelError(string.Empty, "Por favor, ingrese correo y contraseña válidos.");
                return View("Index", loginViewModel);
            }

            try
            {
                // Validar credenciales usando el servicio que llama a la API
                var usuarioValidado = await _homeService.ValidarUsuarioAsync(loginViewModel);

                if (usuarioValidado != null && usuarioValidado.Id > 0) // Validación exitosa
                {
                    _logger.LogInformation("Login exitoso para Usuario ID: {UserId}, Correo: {Correo}, Rol: {Rol}",
                        usuarioValidado.Id, usuarioValidado.Correo, usuarioValidado.Rol);

                    // Guardar datos esenciales en sesión
                    HttpContext.Session.SetInt32("UserId", usuarioValidado.Id);
                    HttpContext.Session.SetString("UserName", $"{usuarioValidado.Nombre} {usuarioValidado.Apellido}");
                    HttpContext.Session.SetString("UserRole", usuarioValidado.Rol ?? "Vendedor"); // Usar Vendedor si el rol es nulo

                    // Redirección basada en rol
                    if (usuarioValidado.Rol?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogInformation("Usuario Admin logueado. Redirigiendo al Dashboard.");
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        _logger.LogInformation("Usuario Vendedor logueado. Redirigiendo a Ventas.");
                        return RedirectToAction("Index", "Venta"); // Vendedores van a Ventas
                    }
                }
                else // Validación fallida (API devolvió null o ID inválido)
                {
                    _logger.LogWarning("Login fallido para {Correo}: Credenciales inválidas según API.", loginViewModel.Correo);
                    ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                    return View("Index", loginViewModel);
                }
            }
            catch (ApplicationException ex) // Error de conexión o API
            {
                _logger.LogError(ex, "Error de aplicación durante el login para {Correo}", loginViewModel.Correo);
                ModelState.AddModelError(string.Empty, ex.Message); // Mostrar mensaje del error de API/conexión
                return View("Index", loginViewModel);
            }
            catch (Exception ex) // Otros errores inesperados
            {
                _logger.LogError(ex, "Error inesperado durante el login para {Correo}", loginViewModel.Correo);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intente de nuevo más tarde.");
                return View("Index", loginViewModel);
            }
        }

        // --- ACCIÓN DE LOGOUT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId"); // Obtener ID antes de limpiar
            _logger.LogInformation("Usuario ID: {UserId} realizando Logout.", userId ?? 0);
            HttpContext.Session.Clear(); // Limpiar todos los datos de la sesión
            return RedirectToAction("Index", "Home"); // Redirigir a Login
        }

        // --- ACCIONES DE REGISTRO ---

        // GET: /Home/Register
        // Muestra el formulario de registro
        [HttpGet]
        public IActionResult Register()
        {
            // Si ya está logueado, no debería poder registrarse de nuevo
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Venta"); // O a donde corresponda
            }
            _logger.LogInformation("Accediendo a la página de registro.");
            return View(new RegisterViewModel());
        }

        // POST: /Home/Register
        // Procesa los datos del formulario de registro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("Intento de registro para el correo: {Correo}", model.Correo);

            // La validación [Compare] en el ViewModel ya verifica si las contraseñas coinciden
            if (ModelState.IsValid)
            {
                // Mapear RegisterViewModel a UsuarioModel (el que espera el servicio)
                // Incluir la Clave para enviarla a la API
                var nuevoUsuario = new Usuario
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    Correo = model.Correo,
                    // Telefono no está en RegisterViewModel, se puede omitir o añadir
                    // Rol se asignará 'Vendedor' por defecto en la API si no se envía
                    // o puedes asignarlo aquí si RegisterViewModel tuviera el campo:
                    // Rol = model.TipoUsuario ?? "Vendedor",
                    Rol = "Admin", // Asignar rol por defecto aquí para claridad
                    Clave = model.Clave // Pasar la contraseña introducida
                };

                try
                {
                    // Llamar al servicio para crear el usuario (enviando la contraseña)
                    var usuarioCreado = await _usuarioService.CreateUsuarioAsync(nuevoUsuario);

                    if (usuarioCreado != null)
                    {
                        _logger.LogInformation("Registro exitoso para el correo: {Correo}. Redirigiendo a Login.", model.Correo);
                        // Mostrar mensaje de éxito en la página de Login
                        TempData["SuccessMessage"] = "¡Registro exitoso! Ahora puedes iniciar sesión.";
                        return RedirectToAction("Index", "Home"); // Redirigir a la página de Login
                    }
                    else
                    {
                        _logger.LogError("La llamada API para registrar usuario {Correo} devolvió null inesperadamente.", model.Correo);
                        ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado durante el registro.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar errores de negocio (ej. correo duplicado)
                {
                    _logger.LogWarning(ex, "Error de operación durante el registro para {Correo}", model.Correo);
                    // Mostrar el mensaje de error específico (ej. "El correo ya existe")
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ApplicationException ex) // Error de conexión con la API
                {
                    _logger.LogError(ex, "Error de conexión con la API durante el registro para {Correo}", model.Correo);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex) // Otros errores inesperados
                {
                    _logger.LogError(ex, "Error inesperado durante el registro para {Correo}", model.Correo);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado durante el registro.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo de registro inválido para el correo: {Correo}", model.Correo);
                // Modelo inválido (ej. contraseñas no coinciden, campos requeridos faltantes)
            }

            // Si llegamos aquí, hubo un error, volver a mostrar el formulario de registro con los errores
            return View(model);
        }
        // ---------------------------------


        public IActionResult Privacy()
        {
            // Podrías añadir verificación de sesión aquí si es necesario
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}