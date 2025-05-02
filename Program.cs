// --- VentasApp/Program.cs ---

using VentasApp.Services; // Namespace de tus servicios
using System;             // Para TimeSpan, Uri, InvalidOperationException
using System.Net.Http.Headers; // Para MediaTypeWithQualityHeaderValue
using Microsoft.AspNetCore.Http; // Para IHttpContextAccessor

var builder = WebApplication.CreateBuilder(args);

// 1. --- Configuración de Servicios ---
builder.Services.AddControllersWithViews(); // Habilita MVC

// Configurar HttpClientFactory para llamar a VentasAPI
builder.Services.AddHttpClient("VentasApiClient", client =>
{
    string? baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("API BaseUrl 'ApiSettings:BaseUrl' not configured in appsettings.json");
    }
    // Construir la URL base para la API, asegurando que termine con /api/
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(60); // Aumentar timeout ligeramente si las operaciones API tardan
});

// Registrar los servicios de la aplicación (uno por cada entidad/funcionalidad)
builder.Services.AddScoped<HomeService>();      // Para Login/Logout/Registro
builder.Services.AddScoped<ClienteService>();   // Para Clientes
builder.Services.AddScoped<ProductoService>();  // Para Productos
builder.Services.AddScoped<UsuarioService>();   // Para Usuarios (gestión y registro)
builder.Services.AddScoped<VentaService>();     // Para Ventas

// Configuración de Sesión en memoria
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de inactividad
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".VentasApp.Session";
});

// Registrar IHttpContextAccessor para poder acceder a la sesión desde el Layout
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

// 2. --- Configuración del Pipeline de Peticiones HTTP ---

// Configuración específica para cada entorno
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Página de error genérica
    app.UseHsts(); // Cabecera HSTS
}
else
{
    app.UseDeveloperExceptionPage(); // Página de error detallada
}

app.UseHttpsRedirection(); // Forzar HTTPS
app.UseStaticFiles(); // Servir archivos de wwwroot (CSS, JS, imágenes)

app.UseRouting(); // Habilitar enrutamiento

// Middleware de Sesión (IMPORTANTE: antes de Auth y Map)
app.UseSession();

// app.UseAuthentication(); // Si se implementa autenticación formal
app.UseAuthorization(); // Habilitar autorización (necesario para roles si se usa [Authorize])

// Mapear las rutas a los controladores MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Ruta por defecto (Login)

// 3. --- Ejecutar la Aplicación ---
app.Run();
