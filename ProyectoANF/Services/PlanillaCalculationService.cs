using ProyectoANF.Models;
using System;

namespace ProyectoANF.Services
{
    public interface IPlanillaCalculationService
    {
        decimal CalcularAguinaldo(decimal salarioBase, double añosAntiguedad, int mes);
        decimal CalcularVacaciones(decimal salarioBase, double añosAntiguedad, int mes);
        decimal CalcularBonoVacaciones(decimal salarioBase, double añosAntiguedad, int mes);
        decimal CalcularHorasDiurnas(decimal salarioBase, decimal cantidadHoras);
        decimal CalcularHorasNocturnas(decimal salarioBase, decimal cantidadHoras);
        decimal CalcularMontoCotizable(decimal salarioBase, decimal vacaciones, decimal bonoVacaciones,
                                       decimal horasDiurnas, decimal horasNocturnas, double añosAntiguedad);
        decimal CalcularIsss(decimal montoCotizable);
        decimal CalcularAfp(decimal montoCotizable);
        decimal CalcularRenta(decimal salarioBruto, decimal isss, decimal afp);
        decimal CalcularBonoAguinaldo(decimal salarioBase, decimal aguinaldo, double añosAntiguedad);

        // Nuevos métodos para incapacidad y permisos
        decimal CalcularMontoIncapacidad(decimal salarioBase, decimal diasIncapacidad, string tipoIncapacidad);
        decimal CalcularMontoPermisos(decimal salarioBase, decimal diasPermisos, string tipoPermiso);
    }

    public class PlanillaCalculationService : IPlanillaCalculationService
    {
        /// <summary>
        /// Calcula el aguinaldo basado en la antigüedad del empleado.
        /// Solo se calcula en diciembre (mes 12).
        /// </summary>
        public decimal CalcularAguinaldo(decimal salarioBase, double añosAntiguedad, int mes)
        {
            if (mes != 12) return 0; // Solo en diciembre

            if (añosAntiguedad < 1)
                return (decimal)añosAntiguedad * (salarioBase / 2);
            else if (añosAntiguedad < 3)
                return salarioBase / 2;
            else if (añosAntiguedad < 10)
                return (salarioBase / 30) * 19;
            else
                return (salarioBase / 30) * 21;
        }

        /// <summary>
        /// Calcula el bono de aguinaldo para empleados con más de 3 años de antigüedad.
        /// </summary>
        public decimal CalcularBonoAguinaldo(decimal salarioBase, decimal aguinaldo, double añosAntiguedad)
        {
            if (añosAntiguedad > 3)
                return salarioBase - aguinaldo;
            return 0;
        }

        /// <summary>
        /// Calcula las vacaciones basadas en la antigüedad del empleado.
        /// Solo se calcula en diciembre (mes 12).
        /// </summary>
        public decimal CalcularVacaciones(decimal salarioBase, double añosAntiguedad, int mes)
        {
            if (mes != 12 || añosAntiguedad < 1) return 0;

            // Base de vacaciones es medio salario (15 días)
            return salarioBase / 2;
        }

        /// <summary>
        /// Calcula el bono de vacaciones basado en la antigüedad.
        /// </summary>
        public decimal CalcularBonoVacaciones(decimal salarioBase, double añosAntiguedad, int mes)
        {
            if (mes != 12 || añosAntiguedad < 1) return 0;

            // Corregido: Aplicar 30% para todos los empleados con derecho a vacaciones (1+ año)
            return (salarioBase / 2) * 0.3m;
        }

        /// <summary>
        /// Calcula el pago por horas extras diurnas.
        /// </summary>
        public decimal CalcularHorasDiurnas(decimal salarioBase, decimal cantidadHoras)
        {
            decimal salarioDiario = salarioBase / 30;
            decimal salarioHora = salarioDiario / 8;

            return cantidadHoras * (salarioHora * 2);
        }

        /// <summary>
        /// Calcula el pago por horas extras nocturnas.
        /// </summary>
        public decimal CalcularHorasNocturnas(decimal salarioBase, decimal cantidadHoras)
        {
            decimal salarioDiario = salarioBase / 30;
            decimal salarioHora = salarioDiario / 8;

            // Mantenido en 2.5 según solicitud
            return cantidadHoras * (salarioHora * 2.5m);
        }

        /// <summary>
        /// Calcula el monto cotizable para deducciones.
        /// </summary>
        public decimal CalcularMontoCotizable(decimal salarioBase, decimal vacaciones, decimal bonoVacaciones,
                                             decimal horasDiurnas, decimal horasNocturnas, double añosAntiguedad)
        {
            // Corregido: Incluir siempre las horas extra, sin importar la antigüedad
            decimal montoCotizable = salarioBase + vacaciones + bonoVacaciones + horasDiurnas + horasNocturnas;

            return montoCotizable;
        }

        /// <summary>
        /// Calcula la deducción del ISSS con el tope de $30 para montos mayores a $1000.
        /// </summary>
        public decimal CalcularIsss(decimal montoCotizable)
        {
            return montoCotizable > 1000 ? 30 : montoCotizable * 0.03m;
        }

        /// <summary>
        /// Calcula la deducción de AFP.
        /// </summary>
        public decimal CalcularAfp(decimal montoCotizable)
        {
            return montoCotizable * 0.0725m;
        }

        /// <summary>
        /// Calcula la retención de renta según tablas de Hacienda.
        /// </summary>
        public decimal CalcularRenta(decimal salarioBruto, decimal isss, decimal afp)
        {
            decimal sobreElExceso = salarioBruto - (isss + afp);

            // Tabla actualizada con la reforma (tramo II ahora inicia en $550 en lugar de $472)
            if (sobreElExceso >= 0.01m && sobreElExceso <= 550)
                return 0;
            else if (sobreElExceso >= 550.01m && sobreElExceso <= 895.24m)
                return (sobreElExceso - 550) * 0.1m + 17.67m;
            else if (sobreElExceso >= 895.25m && sobreElExceso <= 2038.10m)
                return (sobreElExceso - 895.24m) * 0.2m + 60;
            else if (sobreElExceso >= 2038.11m)
                return (sobreElExceso - 2038.10m) * 0.3m + 288.57m;

            return 0;
        }

        /// <summary>
        /// Calcula el monto de incapacidad basado en los días y el tipo.
        /// Para incapacidad común, el empleador paga el 75% del salario por los primeros 3 días.
        /// Para incapacidad profesional, el ISSS cubre desde el primer día.
        /// </summary>
        public decimal CalcularMontoIncapacidad(decimal salarioBase, decimal diasIncapacidad, string tipoIncapacidad)
        {
            decimal salarioDiario = salarioBase / 30;

            if (tipoIncapacidad == "Común")
            {
                // Para incapacidad común, el empleador paga los primeros 3 días al 75%
                decimal diasACargoDeLaEmpresa = Math.Min(diasIncapacidad, 3);
                return diasACargoDeLaEmpresa * (salarioDiario * 0.75m);
            }
            else // "Profesional"
            {
                // Para incapacidad profesional, todo va por cuenta del ISSS
                return 0; // No afecta el salario pagado por la empresa
            }
        }

        /// <summary>
        /// Calcula el monto de permisos basado en los días y el tipo.
        /// Para permisos con goce de sueldo, se paga el 100% del salario.
        /// Para permisos sin goce, no se paga (se descuenta de días trabajados).
        /// </summary>
        public decimal CalcularMontoPermisos(decimal salarioBase, decimal diasPermisos, string tipoPermiso)
        {
            decimal salarioDiario = salarioBase / 30;

            if (tipoPermiso == "Con Goce")
            {
                // Permiso con goce de sueldo: se paga normal
                return diasPermisos * salarioDiario;
            }
            else // "Sin Goce"
            {
                // Permiso sin goce: se descuenta del salario
                return 0; // No suma al salario, se descuenta en días trabajados
            }
        }
    }
}