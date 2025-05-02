using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VentasApp.Models
{
    public class VentaDetalle
    {
        public int Id { get; set; }

        public int IdVenta { get; set; } // FK a Venta

        [Display(Name = "Producto ID")]
        public int IdProducto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Display(Name = "Precio Unit.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecioUnitario { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }

        // Propiedad adicional que viene de la API (con JOINs)
        [Display(Name = "Producto")]
        public string? NombreProducto { get; set; }
    }
}