using System.ComponentModel.DataAnnotations;

namespace VentasApp.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Display(Name = "Nombre/Razón Social")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Apellido { get; set; } // Opcional

        [Display(Name = "Correo Electrónico")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(100)]
        public string? Correo { get; set; }

        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        [StringLength(20)]
        public string? Telefono { get; set; }

        [Display(Name = "Dirección")]
        [StringLength(250)]
        public string? Direccion { get; set; }
    }
}