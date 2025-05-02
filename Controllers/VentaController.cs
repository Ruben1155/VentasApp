using VentasApp.Models;
using VentasApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectList
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// --- Usings para Exportación ---
using iTextSharp.text; // Namespace principal de iTextSharp (versión antigua)
using iTextSharp.text.pdf;
using OfficeOpenXml; // Namespace principal de EPPlus
using System.IO; // Necesario para MemoryStream
// using Microsoft.AspNetCore.Authorization; // Descomentar si implementas autorización formal

namespace VentasApp.Controllers
{
    // Controlador para gestionar las Ventas en la interfaz web
    // [Authorize] // Proteger si es necesario
    public class VentaController : Controller // Podría heredar de AdminBaseController si solo admins ven ventas
    {
        private readonly VentaService _ventaService;
        private readonly ClienteService _clienteService;
        private readonly ProductoService _productoService;
        private readonly ILogger<VentaController> _logger;

        // Inyectar todos los servicios necesarios
        public VentaController(
            VentaService ventaService,
            ClienteService clienteService,
            ProductoService productoService,
            ILogger<VentaController> logger)
        {
            _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _productoService = productoService ?? throw new ArgumentNullException(nameof(productoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Venta o /Venta/Index
        // Muestra la lista de ventas con filtros opcionales
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, int? idClienteFilter)
        {
            _logger.LogInformation("Accediendo a la lista de ventas con filtros: Inicio={StartDate}, Fin={EndDate}, Cliente={ClientId}",
                fechaInicio?.ToShortDateString() ?? "N/A",
                fechaFin?.ToShortDateString() ?? "N/A",
                idClienteFilter?.ToString() ?? "N/A");

            // Guardar filtros para la vista
            ViewData["CurrentFechaInicio"] = fechaInicio?.ToString("yyyy-MM-dd"); // Formato para input date
            ViewData["CurrentFechaFin"] = fechaFin?.ToString("yyyy-MM-dd");
            ViewData["CurrentClienteFilter"] = idClienteFilter;

            IEnumerable<Venta> ventas = new List<Venta>();
            try
            {
                // Cargar lista de clientes para el dropdown de filtro
                await PopulateClientesDropdown(idClienteFilter);
                // Obtener ventas aplicando filtros
                ventas = await _ventaService.GetAllVentasAsync(fechaInicio, fechaFin, idClienteFilter);
            }
            catch (ApplicationException ex) // Error de conexión API
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener ventas o clientes.");
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener la lista de ventas.");
                TempData["ErrorMessage"] = "No se pudo cargar la lista de ventas.";
            }
            // Devolver vista con la lista (posiblemente filtrada o vacía)
            return View(ventas);
        }

        // GET: /Venta/Details/5
        // Muestra los detalles de una venta específica (cabecera y líneas)
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Solicitando detalles para Venta ID: {VentaId}", id);
            if (id <= 0) return BadRequest("ID inválido.");

            try
            {
                // Obtener la cabecera de la venta (necesitamos un GetById en el servicio/API)
                // Solución temporal: Obtener todas y filtrar (ineficiente)
                var ventas = await _ventaService.GetAllVentasAsync(); // TODO: Optimizar creando GetVentaById en API/Service
                var venta = ventas.FirstOrDefault(v => v.Id == id);

                if (venta == null)
                {
                    _logger.LogWarning("Venta ID: {VentaId} no encontrada para ver detalles.", id);
                    TempData["ErrorMessage"] = $"Venta con ID {id} no encontrada.";
                    return NotFound();
                }

                // Obtener los detalles de la venta
                venta.Detalles = (await _ventaService.GetVentaDetallesAsync(id)).ToList();
                _logger.LogInformation("Detalles obtenidos para Venta ID: {VentaId}", id);

                return View(venta); // Pasa la venta con sus detalles a la vista
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al obtener detalles para Venta ID: {VentaId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener detalles para Venta ID: {VentaId}", id);
                TempData["ErrorMessage"] = "Error al cargar los detalles de la venta.";
                return RedirectToAction(nameof(Index));
            }
        }


        // GET: /Venta/Create
        // Muestra el formulario para registrar una nueva venta
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Accediendo al formulario de registro de venta.");
            try
            {
                // Cargar datos necesarios para los dropdowns (Clientes y Productos)
                var viewModel = new VentaCreateViewModel();
                await PopulateVentaCreateDropdowns(viewModel); // Llenar SelectLists en el ViewModel

                return View(viewModel); // Pasar ViewModel a la vista
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error de conexión con la API al preparar formulario de creación de venta.");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar el formulario de creación de venta.");
                TempData["ErrorMessage"] = "Error al cargar datos para el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Venta/Create
        // Procesa el registro de una nueva venta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VentaCreateViewModel ventaViewModel)
        {
            _logger.LogInformation("Intentando registrar nueva venta para Cliente ID: {IdCliente}", ventaViewModel?.IdCliente);

            // Validar que haya al menos un detalle
            if (ventaViewModel.Detalles == null || !ventaViewModel.Detalles.Any())
            {
                ModelState.AddModelError("Detalles", "Debe agregar al menos un producto a la venta.");
                _logger.LogWarning("Intento de crear venta sin detalles para Cliente ID: {IdCliente}", ventaViewModel?.IdCliente);
            }

            // Remover validaciones automáticas de listas que no aplican directamente
            ModelState.Remove("ClientesList");
            ModelState.Remove("ProductosList");

            if (ModelState.IsValid)
            {
                try
                {
                    // Llamar al servicio para registrar la venta
                    var ventaCreada = await _ventaService.CreateVentaAsync(ventaViewModel);

                    if (ventaCreada != null)
                    {
                        _logger.LogInformation("Venta registrada exitosamente via API con ID: {VentaId}", ventaCreada.Id);
                        TempData["SuccessMessage"] = $"Venta #{ventaCreada.Id} registrada correctamente.";
                        return RedirectToAction(nameof(Index)); // Redirigir a la lista de ventas
                    }
                    else
                    {
                        _logger.LogError("La llamada API para registrar venta (Cliente: {IdCliente}) devolvió null inesperadamente.", ventaViewModel.IdCliente);
                        ModelState.AddModelError(string.Empty, "Error inesperado al registrar la venta en el servidor.");
                    }
                }
                catch (InvalidOperationException ex) // Capturar errores de negocio (ej. no stock)
                {
                    _logger.LogWarning(ex, "Error de operación al registrar venta (Cliente: {IdCliente})", ventaViewModel.IdCliente);
                    ModelState.AddModelError(string.Empty, ex.Message); // Mostrar mensaje específico
                }
                catch (ApplicationException ex) // Error de conexión con la API
                {
                    _logger.LogError(ex, "Error de conexión con la API al registrar venta (Cliente: {IdCliente})", ventaViewModel.IdCliente);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception ex) // Otros errores inesperados
                {
                    _logger.LogError(ex, "Error inesperado al registrar venta (Cliente: {IdCliente})", ventaViewModel.IdCliente);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al registrar la venta.");
                }
            }
            else
            {
                _logger.LogWarning("Modelo para registrar venta inválido. Cliente: {IdCliente}", ventaViewModel?.IdCliente);
            }

            // Si hay errores, volver a cargar los dropdowns y mostrar el formulario
            try
            {
                await PopulateVentaCreateDropdowns(ventaViewModel); // Recargar listas
            }
            catch (Exception exDropdown)
            {
                _logger.LogError(exDropdown, "Error al recargar dropdowns después de fallo en creación de venta.");
                ModelState.AddModelError(string.Empty, "Error al recargar datos del formulario.");
            }
            return View(ventaViewModel);
        }


        // --- Métodos Auxiliares ---

        // Carga la lista de clientes para el dropdown del filtro en Index
        private async Task PopulateClientesDropdown(object? selectedCliente = null)
        {
            // Implementación (ya debería estar)
            try
            {
                var clientes = await _clienteService.GetAllClientesAsync();
                ViewBag.ClientesFilterList = new SelectList(
                    clientes.OrderBy(c => c.Nombre).ThenBy(c => c.Apellido),
                    nameof(Cliente.Id),
                    nameof(Cliente.Nombre), // Podría ser Nombre + Apellido
                    selectedCliente);
                _logger.LogDebug("Dropdown de filtro de clientes poblado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al poblar dropdown de filtro de clientes.");
                ViewBag.ClientesFilterList = new SelectList(Enumerable.Empty<SelectListItem>());
                throw; // Re-lanzar para que la acción Index lo maneje
            }
        }

        // Carga las listas de Clientes y Productos para el formulario de Crear Venta
        private async Task PopulateVentaCreateDropdowns(VentaCreateViewModel viewModel, object? selectedCliente = null)
        {
            // Implementación (ya debería estar, incluyendo ViewBag.ProductosInfoJson)
            _logger.LogDebug("Poblando dropdowns y datos de precios para formulario de creación de venta...");
            try
            {
                var clientesTask = _clienteService.GetAllClientesAsync();
                var productosTask = _productoService.GetAllProductosAsync();

                await Task.WhenAll(clientesTask, productosTask);

                var clientes = await clientesTask;
                var productos = await productosTask;

                viewModel.ClientesList = new SelectList(
                    clientes.OrderBy(c => c.Nombre).ThenBy(c => c.Apellido),
                    nameof(Cliente.Id),
                    nameof(Cliente.Nombre),
                    selectedCliente);

                var productosDisponibles = productos.Where(p => p.Existencias > 0).OrderBy(p => p.Nombre).ToList();

                viewModel.ProductosList = new SelectList(
                    productosDisponibles,
                    nameof(Producto.Id),
                    nameof(Producto.Nombre)
                    );

                var productosInfo = productosDisponibles
                                .ToDictionary(p => p.Id.ToString(), p => p.Precio);

                ViewBag.ProductosInfoJson = System.Text.Json.JsonSerializer.Serialize(productosInfo);

                _logger.LogDebug("Dropdowns y datos de precios poblados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al poblar los dropdowns/precios para el formulario de creación de venta.");
                viewModel.ClientesList = new SelectList(Enumerable.Empty<SelectListItem>());
                viewModel.ProductosList = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.ProductosInfoJson = "{}";
                throw new ApplicationException("Error al cargar datos necesarios para el formulario de venta.", ex);
            }
        }

        // --- NUEVAS ACCIONES DE EXPORTACIÓN ---

        // GET: /Venta/ExportToPdf
        public async Task<IActionResult> ExportToPdf(DateTime? fechaInicio, DateTime? fechaFin, int? idClienteFilter)
        {
            _logger.LogInformation("Exportando lista de ventas a PDF con filtros: Inicio={StartDate}, Fin={EndDate}, Cliente={ClientId}",
                fechaInicio?.ToShortDateString() ?? "N/A",
                fechaFin?.ToShortDateString() ?? "N/A",
                idClienteFilter?.ToString() ?? "N/A");
            try
            {
                // Obtener las ventas (filtradas según los parámetros recibidos)
                var ventas = await _ventaService.GetAllVentasAsync(fechaInicio, fechaFin, idClienteFilter);

                using (MemoryStream stream = new MemoryStream())
                {
                    // Usar A4 horizontal si hay muchas columnas o datos largos
                    Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, stream);
                    writer.CloseStream = false;
                    document.Open();

                    // Estilos
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.DARK_GRAY);
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                    Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                    Font totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);

                    // Título
                    Paragraph title = new Paragraph("Reporte de Ventas", titleFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 15f };
                    document.Add(title);

                    // Añadir información de filtros aplicados (opcional)
                    if (fechaInicio.HasValue || fechaFin.HasValue || idClienteFilter.HasValue)
                    {
                        Paragraph filterInfo = new Paragraph("Filtros aplicados:", bodyFont) { SpacingAfter = 5f };
                        if (fechaInicio.HasValue) filterInfo.Add($" Desde: {fechaInicio.Value:dd/MM/yyyy}");
                        if (fechaFin.HasValue) filterInfo.Add($" Hasta: {fechaFin.Value:dd/MM/yyyy}");
                        if (idClienteFilter.HasValue) filterInfo.Add($" Cliente ID: {idClienteFilter.Value}"); // Podrías buscar el nombre del cliente
                        filterInfo.SpacingAfter = 10f;
                        document.Add(filterInfo);
                    }


                    // Tabla
                    PdfPTable table = new PdfPTable(5); // ID, Fecha, Cliente, Vendedor, Total
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1f, 2f, 3f, 3f, 2f }); // Ajustar anchos

                    // Encabezados
                    string[] headers = { "ID", "Fecha", "Cliente", "Vendedor", "Monto Total" };
                    foreach (string header in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                        { BackgroundColor = BaseColor.DARK_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4f }; // Un poco más oscuro
                        table.AddCell(cell);
                    }

                    // Datos
                    decimal montoTotalGeneral = 0;
                    foreach (var venta in ventas)
                    {
                        table.AddCell(new Phrase(venta.Id.ToString(), bodyFont));
                        table.AddCell(new Phrase(venta.FechaVenta.ToString("g"), bodyFont)); // Fecha y hora corta
                        table.AddCell(new Phrase(venta.NombreCliente ?? $"ID: {venta.IdCliente}", bodyFont));
                        table.AddCell(new Phrase(venta.NombreVendedor ?? $"ID: {venta.IdUsuario}", bodyFont));
                        PdfPCell totalCell = new PdfPCell(new Phrase(venta.MontoTotal.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-CR")), bodyFont)) // Formato moneda local
                        { HorizontalAlignment = Element.ALIGN_RIGHT, PaddingRight = 5f };
                        table.AddCell(totalCell);
                        montoTotalGeneral += venta.MontoTotal;
                    }

                    document.Add(table);

                    // Añadir Total General al final
                    Paragraph totalGeneral = new Paragraph($"Monto Total General: {montoTotalGeneral.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-CR"))}", totalFont)
                    { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 10f };
                    document.Add(totalGeneral);


                    document.Close();
                    stream.Position = 0;
                    _logger.LogInformation("PDF de ventas generado exitosamente.");
                    return File(stream.ToArray(), "application/pdf", "ReporteVentas.pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF de ventas.");
                TempData["ErrorMessage"] = "Error al generar el archivo PDF de ventas.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Venta/ExportToExcel
        public async Task<IActionResult> ExportToExcel(DateTime? fechaInicio, DateTime? fechaFin, int? idClienteFilter)
        {
            _logger.LogInformation("Exportando lista de ventas a Excel con filtros: Inicio={StartDate}, Fin={EndDate}, Cliente={ClientId}",
               fechaInicio?.ToShortDateString() ?? "N/A",
               fechaFin?.ToShortDateString() ?? "N/A",
               idClienteFilter?.ToString() ?? "N/A");
            try
            {
                var ventas = await _ventaService.GetAllVentasAsync(fechaInicio, fechaFin, idClienteFilter);

                using (MemoryStream stream = new MemoryStream())
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Ventas");

                        // Encabezados
                        worksheet.Cells[1, 1].Value = "ID Venta";
                        worksheet.Cells[1, 2].Value = "Fecha Venta";
                        worksheet.Cells[1, 3].Value = "Cliente";
                        worksheet.Cells[1, 4].Value = "Vendedor";
                        worksheet.Cells[1, 5].Value = "Monto Total";

                        // Aplicar estilo a encabezados
                        using (var range = worksheet.Cells[1, 1, 1, 5])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }

                        // Datos
                        int row = 2;
                        decimal montoTotalGeneral = 0;
                        foreach (var venta in ventas)
                        {
                            worksheet.Cells[row, 1].Value = venta.Id;
                            worksheet.Cells[row, 2].Value = venta.FechaVenta;
                            worksheet.Cells[row, 2].Style.Numberformat.Format = "dd/mm/yyyy hh:mm"; // Formato fecha/hora
                            worksheet.Cells[row, 3].Value = venta.NombreCliente ?? $"ID: {venta.IdCliente}";
                            worksheet.Cells[row, 4].Value = venta.NombreVendedor ?? $"ID: {venta.IdUsuario}";
                            worksheet.Cells[row, 5].Value = venta.MontoTotal;
                            worksheet.Cells[row, 5].Style.Numberformat.Format = "₡#,##0.00"; // Formato moneda local (Colones)

                            montoTotalGeneral += venta.MontoTotal;
                            row++;
                        }

                        // Añadir fila de total general
                        worksheet.Cells[row, 4].Value = "Total General:";
                        worksheet.Cells[row, 4].Style.Font.Bold = true;
                        worksheet.Cells[row, 5].Value = montoTotalGeneral;
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "₡#,##0.00";
                        worksheet.Cells[row, 5].Style.Font.Bold = true;


                        // Autoajustar columnas
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        package.Save();
                    }

                    stream.Position = 0;
                    _logger.LogInformation("Archivo Excel de ventas generado exitosamente.");
                    string excelName = $"ReporteVentas-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar Excel de ventas.");
                TempData["ErrorMessage"] = "Error al generar el archivo Excel de ventas.";
                return RedirectToAction(nameof(Index));
            }
        }


    } // Fin de la clase VentaController
} // Fin del namespace
