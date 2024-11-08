namespace ProyectoANF.ViewModel
{
    public class UsuarioVM
    {
        

        public string NombreUsuario { get; set; } = null!;

        public string Contraseña { get; set; } = null!;
        
        public string ConfirmarContraseña { get; set; } = null!;

        public string Rol { get; set; } = "Usuario";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
