using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoANF.Models;

public partial class Trabajadore
{
    public int TrabajadorId { get; set; }
    [Required]
    public string Nombre { get; set; } = null!;
    [Required]
    public string Dui { get; set; } = null!;
    [Required]
    public string Nit { get; set; } = null!;
    [Required]
    public string Afp { get; set; } = null!;
    [Required]
    public string Isss { get; set; } = null!;
    [Required]
    public string? Cargo { get; set; }
    [Required]
    public decimal SalarioBase { get; set; }
    [Required]
    public DateOnly FechaContratacion { get; set; }
    [Required]
    public bool Activo { get; set; } = true;

    public virtual ICollection<Planilla> Planillas { get; set; } = new List<Planilla>();
}
