using System;
using System.Collections.Generic;

namespace ProyectoANF.Models;

public partial class Planilla
{
    public int PlanillaId { get; set; }

    public int? TrabajadorId { get; set; }

    public int Mes { get; set; }

    public int Año { get; set; }

    public int DiasTrabajados { get; set; }

    public decimal? SalarioBruto { get; set; }

    public decimal? Isss { get; set; }

    public decimal? Afp { get; set; }

    public decimal? Renta { get; set; }

    public decimal? SalarioNeto { get; set; }

    public DateTime? FechaGeneracion { get; set; }

    public virtual Trabajadore? Trabajador { get; set; }
}
