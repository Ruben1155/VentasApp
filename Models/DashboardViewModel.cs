using System.ComponentModel.DataAnnotations;

namespace VentasApp.Models
{
    public class DashboardViewModel
    {
        [Display(Name = "Total Clientes")] // Cambiado de Usuarios a Clientes
        public int TotalClientes { get; set; }

        [Display(Name = "Total Productos")]
        public int TotalProductos { get; set; }

        [Display(Name = "Ventas Registradas (Hoy)")] // Ejemplo de otro contador
        public int VentasHoy { get; set; }

        [Display(Name = "Monto Total Vendido (Hoy)")] // Ejemplo
        [DataType(DataType.Currency)]
        public decimal MontoVentasHoy { get; set; }
    }
}