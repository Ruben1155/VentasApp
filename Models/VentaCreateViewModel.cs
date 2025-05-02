using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectList

namespace VentasApp.Models
{
    public class VentaCreateViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente.")]
        [Display(Name = "Cliente")]
        public int IdCliente { get; set; }

        // Lista para llenar el dropdown de clientes
        public SelectList? ClientesList { get; set; }

        // Lista para manejar los detalles de la venta en el formulario
        // Usaremos JavaScript para añadir/quitar productos dinámicamente
        public List<VentaDetalleViewModel> Detalles { get; set; } = new List<VentaDetalleViewModel>();

        // Lista de productos disponibles para añadir (para el selector/búsqueda)
        public SelectList? ProductosList { get; set; }
    }

    // Modelo simple para representar un detalle en el formulario
    public class VentaDetalleViewModel
    {
        [Required]
        public int IdProducto { get; set; }
        public string? NombreProducto { get; set; } // Para mostrar
        [Required]
        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; } // Se obtendrá al seleccionar producto
        public decimal Subtotal => Cantidad * PrecioUnitario; // Calculado
    }
}