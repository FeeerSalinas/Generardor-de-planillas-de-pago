using System;
using System.Collections.Generic;

namespace ProyectoANF.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public string Rol { get; set; } = "Usuario";

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
