using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
    public decimal? HorasDiurnas { get; set; }

    public decimal? HorasDiurnasCantidad { get; set; }

    public decimal? HorasNocturnasCantidad { get; set; }
    public decimal? HorasNocturnas { get; set; }
    public decimal? Vacaciones { get; set; }
    public decimal? Indemnizacion { get; set; }
    public decimal? Aguinaldo { get; set; }
    public DateTime? FechaGeneracion { get; set; }

    public decimal? Feriado { get; set; }

    public decimal? FeriadoCantidad { get; set; }

    public decimal? Reintegro { get; set; }

    public decimal? Incapacidad { get; set; }

    public decimal? Permisos { get; set; }

    // Nuevas propiedades añadidas
    public decimal? IncapacidadDias { get; set; }
    public decimal? PermisosDias { get; set; }
    public string? IncapacidadTipo { get; set; } = "Común";
    public string? PermisosTipo { get; set; } = "Con Goce";

    public virtual Trabajadore? Trabajador { get; set; }
}