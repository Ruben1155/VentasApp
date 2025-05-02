using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentasApp.Models
{
    public class Venta
    {
        public int Id { get; set; }

        [Display(Name = "Fecha Venta")]
        [DataType(DataType.DateTime)]
        public DateTime FechaVenta { get; set; }

        [Display(Name = "Cliente ID")]
        public int IdCliente { get; set; }

        [Display(Name = "Vendedor ID")]
        public int IdUsuario { get; set; }

        [Display(Name = "Monto Total")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MontoTotal { get; set; }

        // Propiedades adicionales que vienen de la API (con JOINs)
        [Display(Name = "Cliente")]
        public string? NombreCliente { get; set; }

        [Display(Name = "Vendedor")]
        public string? NombreVendedor { get; set; }

        // Lista de detalles (podría llenarse si se hace una llamada adicional)
        public List<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    }
}