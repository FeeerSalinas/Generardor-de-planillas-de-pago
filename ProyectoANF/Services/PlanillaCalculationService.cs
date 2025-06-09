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

            if (añosAntiguedad <= 2)
                return 0;
            else if (añosAntiguedad < 5)
                return (salarioBase / 2) * 0.05m;
            else
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

            return cantidadHoras * (salarioHora * 2.5m);
        }

        /// <summary>
        /// Calcula el monto cotizable para deducciones.
        /// </summary>
        public decimal CalcularMontoCotizable(decimal salarioBase, decimal vacaciones, decimal bonoVacaciones,
                                             decimal horasDiurnas, decimal horasNocturnas, double añosAntiguedad)
        {
            decimal montoCotizable = salarioBase + vacaciones + bonoVacaciones;

            // Si tiene más de 8 años, incluir horas extra en el monto cotizable
            if (añosAntiguedad > 8)
            {
                montoCotizable += horasDiurnas + horasNocturnas;
            }

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

            if (sobreElExceso >= 0.01m && sobreElExceso <= 472)
                return 0;
            else if (sobreElExceso >= 472.01m && sobreElExceso <= 895.24m)
                return (sobreElExceso - 472) * 0.1m + 17.67m;
            else if (sobreElExceso >= 895.25m && sobreElExceso <= 2038.10m)
                return (sobreElExceso - 895.24m) * 0.2m + 60;
            else if (sobreElExceso >= 2038.11m)
                return (sobreElExceso - 2038.10m) * 0.3m + 288.57m;

            return 0;
        }
    }
}