using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoANF.Models.ViewModels
{
    public class ViewModelPlanilla
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

        [NotMapped]

        public int? HorasDiurnasCantidad { get; set; }

        [NotMapped]

        public int? HorasNocturnasCantidad { get; set; }

        public decimal? HorasNocturnas { get; set; }

        public decimal? Vacaciones { get; set; }

        public decimal? Indemnizacion { get; set; }

        public decimal? Aguinaldo { get; set; }

        public DateTime? FechaGeneracion { get; set; }

        public virtual Trabajadore? Trabajador { get; set; }

        public List<ViewModelTrabajadore> empleados { get; set; }
    }

    public class ViewModelTrabajadore
    {
        public int TrabajadorId { get; set; }
        
        public string Nombre { get; set; } = null!;
       
        public string Dui { get; set; } = null!;
       
        public string Nit { get; set; } = null!;
        
        public string Afp { get; set; } = null!;
       
        public string Isss { get; set; } = null!;
       
        public string? Cargo { get; set; }
        
        public decimal SalarioBase { get; set; }
       
        public DateOnly FechaContratacion { get; set; }
        
        public bool Activo { get; set; } = true;

        public virtual ICollection<Planilla> Planillas { get; set; } = new List<Planilla>();
    }
}
