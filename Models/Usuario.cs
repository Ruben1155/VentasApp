using System.ComponentModel.DataAnnotations;

namespace VentasApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellido { get; set; } = string.Empty;

        [Display(Name = "Correo Electrónico")]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string Correo { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string Rol { get; set; } = string.Empty;

        // Propiedad para pasar la contraseña SÓLO durante el registro
        // No se usa para mostrar ni para editar perfil.
        // Se mapeará desde RegisterViewModel.
        public string? Clave { get; set; }
    }
}