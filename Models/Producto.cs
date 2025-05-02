using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VentasApp.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        [DataType(DataType.MultilineText)] // Sugerencia para la vista
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [DataType(DataType.Currency)] // Sugerencia para formato
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser positivo.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "Las existencias son obligatorias.")]
        [Range(0, int.MaxValue, ErrorMessage = "Las existencias no pueden ser negativas.")]
        public int Existencias { get; set; }
    }
}