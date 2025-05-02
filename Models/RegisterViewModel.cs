using System.ComponentModel.DataAnnotations;

namespace VentasApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [Display(Name = "Apellido")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [Display(Name = "Correo Electrónico")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        // Teléfono opcional en el registro
        // [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
        // [Display(Name = "Teléfono (Opcional)")]
        // [StringLength(20)]
        // public string? Telefono { get; set; }

        // Rol se asigna por defecto 'Vendedor' en la API al registrarse
        // No es necesario pedirlo aquí, a menos que quieras permitir elegir entre Estudiante/Docente/Otro
        // [Required(ErrorMessage = "El tipo de usuario es obligatorio.")]
        // [Display(Name = "Tipo de Usuario")]
        // public string TipoUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)] // Ajustar longitud mínima si se desea
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Clave { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Clave", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string ConfirmarClave { get; set; } = string.Empty;
    }
}