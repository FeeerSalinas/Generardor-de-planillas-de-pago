using iText.Kernel.Pdf;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoANF.Models;
using ProyectoANF.Models.ViewModels;
using System.Numerics;
using Rotativa.AspNetCore;
using ProyectoANF.Services;

namespace ProyectoANF.Controllers
{
    public class PlanillaController : Controller
    {
        private readonly PlanillaPagoDbContext _context;
        private readonly ILogger<PlanillaController> _logger;
        private readonly IEmailService _emailService;
        private readonly IPlanillaCalculationService _calculationService;

        public PlanillaController(PlanillaPagoDbContext context, ILogger<PlanillaController> logger,
                                  IEmailService emailService, IPlanillaCalculationService calculationService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _calculationService = calculationService;
        }

        public async Task<IActionResult> ListaPlanillas(int? pageNumber)
        {
            int pageSize = 5;
            var planillas = _context.Planillas
                .Include(p => p.Trabajador)
                .OrderByDescending(p => p.FechaGeneracion);

            // Calcular totales
            var todasLasPlanillas = await _context.Planillas
                .Include(p => p.Trabajador)
                .ToListAsync();

            ViewData["Totales"] = new
            {
                TotalSalarioBase = todasLasPlanillas.Sum(p => p.Trabajador?.SalarioBase ?? 0),
                TotalViaticos = todasLasPlanillas.Sum(p => p.Feriado ?? 0), // Usando el campo Feriado como equivalente a Viáticos
                TotalHorasDiurnas = todasLasPlanillas.Sum(p => p.HorasDiurnas ?? 0),
                TotalHorasNocturnas = todasLasPlanillas.Sum(p => p.HorasNocturnas ?? 0),
                TotalVacaciones = todasLasPlanillas.Sum(p => p.Vacaciones ?? 0),
                // Para Bono Vacaciones, lo calculamos ya que no está almacenado explícitamente
                TotalBonoVacaciones = todasLasPlanillas.Sum(p => {
                    var trabajador = p.Trabajador;
                    if (trabajador != null && p.Mes == 12)
                    {
                        // Calcular tiempo en empresa exactamente como Excel: (E6-D6)/365
                        var fechaCorte = new DateOnly(p.Año, p.Mes, DateTime.DaysInMonth(p.Año, p.Mes));
                        double añosAntiguedad = (fechaCorte.ToDateTime(TimeOnly.MinValue) -
                                               trabajador.FechaContratacion.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.0;

                        if (añosAntiguedad <= 2)
                            return 0;
                        else if (añosAntiguedad < 5)
                            return (trabajador.SalarioBase / 2) * 0.05m;
                        else
                            return (trabajador.SalarioBase / 2) * 0.3m;
                    }
                    return 0;
                }),
                TotalAguinaldo = todasLasPlanillas.Sum(p => p.Aguinaldo ?? 0),
                // Para Bono Aguinaldo, también lo calculamos
                TotalBonoAguinaldo = todasLasPlanillas.Sum(p => {
                    var trabajador = p.Trabajador;
                    if (trabajador != null && p.Mes == 12)
                    {
                        // Calcular tiempo en empresa exactamente como Excel: (E6-D6)/365
                        var fechaCorte = new DateOnly(p.Año, p.Mes, DateTime.DaysInMonth(p.Año, p.Mes));
                        double añosAntiguedad = (fechaCorte.ToDateTime(TimeOnly.MinValue) -
                                               trabajador.FechaContratacion.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.0;

                        if (añosAntiguedad > 3)
                            return trabajador.SalarioBase - (p.Aguinaldo ?? 0);
                        return 0;
                    }
                    return 0;
                }),
                TotalMontoCotizable = todasLasPlanillas.Sum(p => p.SalarioBruto ?? 0),
                TotalIsssPatronal = todasLasPlanillas.Sum(p => (p.SalarioBruto ?? 0) > 1000 ? 75 : (p.SalarioBruto ?? 0) * 0.075m),
                TotalAfpPatronal = todasLasPlanillas.Sum(p => (p.SalarioBruto ?? 0) * 0.0875m),
                TotalIsssEmpleado = todasLasPlanillas.Sum(p => p.Isss ?? 0),
                TotalAfpEmpleado = todasLasPlanillas.Sum(p => p.Afp ?? 0),
                TotalDepositar = todasLasPlanillas.Sum(p => p.SalarioNeto ?? 0),
                TotalPlanillaUnica = todasLasPlanillas.Sum(p => {
                    decimal isssPatronal = (p.SalarioBruto ?? 0) > 1000 ? 75 : (p.SalarioBruto ?? 0) * 0.075m;
                    decimal afpPatronal = (p.SalarioBruto ?? 0) * 0.0875m;
                    return isssPatronal + afpPatronal + (p.Isss ?? 0) + (p.Afp ?? 0);
                })
            };

            return View(await PaginatedList<Planilla>.CreateAsync(planillas.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [HttpGet]
        public async Task<IActionResult> CrearPlanilla()
        {
            //obtener una lista de trabajadores
            ViewData["Trabajadores"] = await _context.Trabajadores.Where(t => t.Activo).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearPlanilla(Planilla modelo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var trabajador = await _context.Trabajadores.FirstOrDefaultAsync(t => t.TrabajadorId == modelo.TrabajadorId);
                    if (trabajador == null)
                    {
                        ViewData["Error"] = "El trabajador seleccionado no existe";
                        return View(modelo);
                    }

                    // Calcular la antigüedad del trabajador exactamente como en Excel: (E6-D6)/365
                    var fechaCorte = new DateOnly(modelo.Año, modelo.Mes, DateTime.DaysInMonth(modelo.Año, modelo.Mes));
                    double añosAntiguedad = (fechaCorte.ToDateTime(TimeOnly.MinValue) -
                                           trabajador.FechaContratacion.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.0;

                    // Calcular el mes exactamente como en Excel: MONTH(D6)
                    int mesDeIngreso = trabajador.FechaContratacion.Month;

                    // Usar el servicio de cálculos para aplicar las fórmulas del Excel
                    decimal salarioBase = trabajador.SalarioBase;

                    // Calcular horas extras
                    modelo.HorasDiurnas = _calculationService.CalcularHorasDiurnas(salarioBase, modelo.HorasDiurnasCantidad ?? 0);
                    modelo.HorasNocturnas = _calculationService.CalcularHorasNocturnas(salarioBase, modelo.HorasNocturnasCantidad ?? 0);

                    // Calcular vacaciones y bono de vacaciones (si los campos no fueron especificados manualmente)
                    if (!modelo.Vacaciones.HasValue || modelo.Vacaciones == 0)
                    {
                        // Solo en diciembre y solo si el mes de ingreso es 12 (como en Excel)
                        if (modelo.Mes == 12 && mesDeIngreso == 12)
                        {
                            modelo.Vacaciones = _calculationService.CalcularVacaciones(salarioBase, añosAntiguedad, modelo.Mes);
                        }
                        else
                        {
                            modelo.Vacaciones = 0;
                        }
                    }

                    // Calcular aguinaldo (si no fue especificado manualmente)
                    if (!modelo.Aguinaldo.HasValue || modelo.Aguinaldo == 0)
                    {
                        // Solo en diciembre y solo si el mes de ingreso es 12 (como en Excel)
                        if (modelo.Mes == 12 && mesDeIngreso == 12)
                        {
                            modelo.Aguinaldo = _calculationService.CalcularAguinaldo(salarioBase, añosAntiguedad, modelo.Mes);
                        }
                        else
                        {
                            modelo.Aguinaldo = 0;
                        }
                    }

                    // Calcular bono aguinaldo si aplica (solo en diciembre y mes de ingreso 12)
                    if (modelo.Mes == 12 && mesDeIngreso == 12 && añosAntiguedad > 3)
                    {
                        modelo.Feriado = modelo.FeriadoCantidad * (salarioBase / 30 / 8 * 2);
                    }

                    // Calcular el salario bruto incluyendo todos los componentes
                    decimal salarioDiario = salarioBase / 30;
                    modelo.SalarioBruto = (salarioDiario * modelo.DiasTrabajados) +
                                          (modelo.HorasDiurnas ?? 0) +
                                          (modelo.HorasNocturnas ?? 0) +
                                          (modelo.Feriado ?? 0) +
                                          (modelo.Vacaciones ?? 0) +
                                          (modelo.Aguinaldo ?? 0) +
                                          (modelo.Indemnizacion ?? 0) +
                                          (modelo.Reintegro ?? 0);

                    // Calcular monto cotizable para deducciones
                    decimal bonoVacaciones = 0;
                    if (modelo.Mes == 12 && mesDeIngreso == 12)
                    {
                        bonoVacaciones = _calculationService.CalcularBonoVacaciones(salarioBase, añosAntiguedad, modelo.Mes);
                    }

                    decimal montoCotizable = _calculationService.CalcularMontoCotizable(
                        salarioBase,
                        modelo.Vacaciones ?? 0,
                        bonoVacaciones,
                        modelo.HorasDiurnas ?? 0,
                        modelo.HorasNocturnas ?? 0,
                        añosAntiguedad
                    );

                    // Calcular deducciones
                    modelo.Isss = _calculationService.CalcularIsss(montoCotizable);
                    modelo.Afp = _calculationService.CalcularAfp(montoCotizable);
                    modelo.Renta = _calculationService.CalcularRenta(modelo.SalarioBruto ?? 0, modelo.Isss ?? 0, modelo.Afp ?? 0);

                    // Calcular salario neto
                    modelo.SalarioNeto = modelo.SalarioBruto - modelo.Isss - modelo.Afp - modelo.Renta;
                    modelo.FechaGeneracion = DateTime.Now;

                    await _context.Planillas.AddAsync(modelo);
                    await _context.SaveChangesAsync();

                    ViewData["Completado"] = "Planilla registrada con éxito";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ViewData["Error"] = $"Ocurrió un error al registrar la planilla: {ex.Message}";
                    return View(modelo);
                }
            }

            ViewData["Trabajadores"] = await _context.Trabajadores.Where(t => t.Activo).ToListAsync();
            ViewData["Error"] = "Por favor corrija los errores en el formulario.";
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> EditarPlanilla(int id)
        {
            var planilla = await _context.Planillas.FindAsync(id);
            if (planilla == null)
            {
                return NotFound();
            }

            ViewData["Trabajadores"] = await _context.Trabajadores.Where(t => t.Activo).ToListAsync();
            return View(planilla);
        }

        [HttpPost]
        public async Task<IActionResult> EditarPlanilla(Planilla modelo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var planillaExistente = await _context.Planillas.FindAsync(modelo.PlanillaId);
                    if (planillaExistente == null)
                    {
                        ViewData["Error"] = "La planilla no existe.";
                        return View(modelo);
                    }

                    var trabajador = await _context.Trabajadores.FirstOrDefaultAsync(t => t.TrabajadorId == modelo.TrabajadorId);
                    if (trabajador == null)
                    {
                        ViewData["Error"] = "El trabajador asociado a esta planilla no existe.";
                        return View(modelo);
                    }

                    // Calcular la antigüedad del trabajador exactamente como en Excel: (E6-D6)/365
                    var fechaCorte = new DateOnly(modelo.Año, modelo.Mes, DateTime.DaysInMonth(modelo.Año, modelo.Mes));
                    double añosAntiguedad = (fechaCorte.ToDateTime(TimeOnly.MinValue) -
                                           trabajador.FechaContratacion.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.0;

                    // Calcular el mes exactamente como en Excel: MONTH(D6)
                    int mesDeIngreso = trabajador.FechaContratacion.Month;

                    // Usar el servicio de cálculos para aplicar las fórmulas del Excel
                    decimal salarioBase = trabajador.SalarioBase;

                    // Actualizar los campos de la planilla existente
                    planillaExistente.Mes = modelo.Mes;
                    planillaExistente.Año = modelo.Año;
                    planillaExistente.DiasTrabajados = modelo.DiasTrabajados;
                    planillaExistente.HorasDiurnasCantidad = modelo.HorasDiurnasCantidad;
                    planillaExistente.HorasNocturnasCantidad = modelo.HorasNocturnasCantidad;
                    planillaExistente.FeriadoCantidad = modelo.FeriadoCantidad;
                    planillaExistente.Reintegro = modelo.Reintegro;
                    planillaExistente.Incapacidad = modelo.Incapacidad;
                    planillaExistente.Permisos = modelo.Permisos;

                    // Calcular horas extras
                    planillaExistente.HorasDiurnas = _calculationService.CalcularHorasDiurnas(salarioBase, modelo.HorasDiurnasCantidad ?? 0);
                    planillaExistente.HorasNocturnas = _calculationService.CalcularHorasNocturnas(salarioBase, modelo.HorasNocturnasCantidad ?? 0);

                    // Calcular feriado
                    planillaExistente.Feriado = modelo.FeriadoCantidad * (salarioBase / 30 / 8 * 2);

                    // Calcular vacaciones, aguinaldo e indemnización
                    // Permitir la edición manual, pero si están vacíos, calcularlos automáticamente
                    if (modelo.Vacaciones.HasValue && modelo.Vacaciones > 0)
                    {
                        planillaExistente.Vacaciones = modelo.Vacaciones;
                    }
                    else
                    {
                        // Solo en diciembre y solo si el mes de ingreso es 12 (como en Excel)
                        if (modelo.Mes == 12 && mesDeIngreso == 12)
                        {
                            planillaExistente.Vacaciones = _calculationService.CalcularVacaciones(salarioBase, añosAntiguedad, modelo.Mes);
                        }
                        else
                        {
                            planillaExistente.Vacaciones = 0;
                        }
                    }

                    if (modelo.Aguinaldo.HasValue && modelo.Aguinaldo > 0)
                    {
                        planillaExistente.Aguinaldo = modelo.Aguinaldo;
                    }
                    else
                    {
                        // Solo en diciembre y solo si el mes de ingreso es 12 (como en Excel)
                        if (modelo.Mes == 12 && mesDeIngreso == 12)
                        {
                            planillaExistente.Aguinaldo = _calculationService.CalcularAguinaldo(salarioBase, añosAntiguedad, modelo.Mes);
                        }
                        else
                        {
                            planillaExistente.Aguinaldo = 0;
                        }
                    }

                    if (modelo.Indemnizacion.HasValue && modelo.Indemnizacion > 0)
                    {
                        planillaExistente.Indemnizacion = modelo.Indemnizacion;
                    }

                    // Calcular el salario bruto incluyendo todos los componentes
                    decimal salarioDiario = salarioBase / 30;
                    planillaExistente.SalarioBruto = (salarioDiario * planillaExistente.DiasTrabajados) +
                                          (planillaExistente.HorasDiurnas ?? 0) +
                                          (planillaExistente.HorasNocturnas ?? 0) +
                                          (planillaExistente.Feriado ?? 0) +
                                          (planillaExistente.Vacaciones ?? 0) +
                                          (planillaExistente.Aguinaldo ?? 0) +
                                          (planillaExistente.Indemnizacion ?? 0) +
                                          (planillaExistente.Reintegro ?? 0);

                    // Calcular monto cotizable para deducciones
                    decimal bonoVacaciones = 0;
                    if (modelo.Mes == 12 && mesDeIngreso == 12)
                    {
                        bonoVacaciones = _calculationService.CalcularBonoVacaciones(salarioBase, añosAntiguedad, modelo.Mes);
                    }

                    decimal montoCotizable = _calculationService.CalcularMontoCotizable(
                        salarioBase,
                        planillaExistente.Vacaciones ?? 0,
                        bonoVacaciones,
                        planillaExistente.HorasDiurnas ?? 0,
                        planillaExistente.HorasNocturnas ?? 0,
                        añosAntiguedad
                    );

                    // Calcular deducciones
                    planillaExistente.Isss = _calculationService.CalcularIsss(montoCotizable);
                    planillaExistente.Afp = _calculationService.CalcularAfp(montoCotizable);
                    planillaExistente.Renta = _calculationService.CalcularRenta(
                        planillaExistente.SalarioBruto ?? 0,
                        planillaExistente.Isss ?? 0,
                        planillaExistente.Afp ?? 0
                    );

                    // Calcular salario neto
                    planillaExistente.SalarioNeto = planillaExistente.SalarioBruto -
                                                   planillaExistente.Isss -
                                                   planillaExistente.Afp -
                                                   planillaExistente.Renta;

                    await _context.SaveChangesAsync();
                    return RedirectToAction("ListaPlanillas");
                }
                catch (Exception ex)
                {
                    ViewData["Error"] = $"Ocurrió un error al actualizar la planilla: {ex.Message}";
                    return View(modelo);
                }
            }

            ViewData["Trabajadores"] = await _context.Trabajadores.Where(t => t.Activo).ToListAsync();
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> GenerarPDF(int id)
        {
            var planilla = await _context.Planillas
                .Include(p => p.Trabajador)
                .FirstOrDefaultAsync(p => p.PlanillaId == id);

            if (planilla == null)
            {
                return NotFound();
            }

            // Generate PDF using Rotativa
            return new ViewAsPdf("GenerarPDF", planilla)
            {
                FileName = $"Planilla-{planilla.PlanillaId}-{planilla.Trabajador?.Nombre}-{planilla.Mes}-{planilla.Año}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(20, 20, 20, 20),
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--enable-local-file-access"
            };
        }

        [HttpGet]
        public async Task<IActionResult> EnviarPlanillaPorCorreo(int id)
        {
            var planilla = await _context.Planillas
                .Include(p => p.Trabajador)
                .FirstOrDefaultAsync(p => p.PlanillaId == id);

            if (planilla == null || planilla.Trabajador == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(planilla.Trabajador.Correo) || planilla.Trabajador.Correo == "No proporcionado")
            {
                TempData["Error"] = "El trabajador no tiene un correo electrónico registrado.";
                return RedirectToAction(nameof(ListaPlanillas));
            }

            try
            {
                // Generar el PDF
                var pdfResult = new ViewAsPdf("GenerarPDF", planilla)
                {
                    FileName = $"Planilla-{planilla.PlanillaId}-{planilla.Trabajador.Nombre}-{planilla.Mes}-{planilla.Año}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageMargins = new Rotativa.AspNetCore.Options.Margins(20, 20, 20, 20),
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                    CustomSwitches = "--enable-local-file-access"
                };

                // Convertir el PDF a bytes
                var binary = await pdfResult.BuildFile(ControllerContext);

                // Preparar el correo
                var subject = $"Planilla de Pago - {planilla.Mes}/{planilla.Año}";
                var body = $@"
                <h2>Planilla de Pago</h2>
                <p>Estimado/a {planilla.Trabajador.Nombre},</p>
                <p>Adjunto encontrará su planilla de pago correspondiente al período {planilla.Mes}/{planilla.Año}.</p>
                <p>Saludos cordiales.</p>";

                // Enviar el correo
                await _emailService.SendEmailWithAttachmentAsync(
                    planilla.Trabajador.Correo,
                    subject,
                    body,
                    binary,
                    pdfResult.FileName);

                TempData["Success"] = "La planilla ha sido enviada exitosamente por correo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al enviar la planilla por correo: " + ex.Message;
            }

            return RedirectToAction(nameof(ListaPlanillas));
        }

        // Este método ya no se utilizará, pero se mantiene para compatibilidad con código existente
        private void CompletarCalculosPlanilla(Planilla modelo, Trabajadore trabajador)
        {
            // Calcular antigüedad exactamente como en Excel (E6-D6)/365
            var fechaCorte = new DateOnly(modelo.Año, modelo.Mes, DateTime.DaysInMonth(modelo.Año, modelo.Mes));
            double añosAntiguedad = (fechaCorte.ToDateTime(TimeOnly.MinValue) -
                                   trabajador.FechaContratacion.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.0;

            // Calcular el mes exactamente como en Excel: MONTH(D6)
            int mesDeIngreso = trabajador.FechaContratacion.Month;

            decimal salarioBase = trabajador.SalarioBase;
            decimal salarioDiario = salarioBase / 30;
            decimal salarioHora = salarioDiario / 8;

            // Horas y feriado
            modelo.HorasDiurnas ??= 0;
            modelo.HorasNocturnas ??= 0;
            modelo.Feriado ??= 0;

            modelo.HorasDiurnas = modelo.HorasDiurnasCantidad * (salarioHora * 2);
            modelo.HorasNocturnas = modelo.HorasNocturnasCantidad * (salarioHora * 2.5m);
            modelo.Feriado = modelo.FeriadoCantidad * (salarioHora * 2);

            // Vacaciones (solo en diciembre y solo si el mes de ingreso es 12, para empleados con más de 1 año)
            if (modelo.Mes == 12 && mesDeIngreso == 12 && añosAntiguedad >= 1)
            {
                // Solo aplicar si no se ha especificado manualmente
                if (!modelo.Vacaciones.HasValue || modelo.Vacaciones == 0)
                {
                    modelo.Vacaciones = salarioBase / 2;

                    // Bono de vacaciones según antigüedad
                    if (añosAntiguedad > 5)
                        modelo.Vacaciones += (salarioBase / 2) * 0.3m;
                    else if (añosAntiguedad >= 2)
                        modelo.Vacaciones += (salarioBase / 2) * 0.05m;
                }
            }
            else
            {
                modelo.Vacaciones = 0;
            }

            // Aguinaldo (solo en diciembre y solo si el mes de ingreso es 12)
            if (modelo.Mes == 12 && mesDeIngreso == 12)
            {
                // Solo aplicar si no se ha especificado manualmente
                if (!modelo.Aguinaldo.HasValue || modelo.Aguinaldo == 0)
                {
                    if (añosAntiguedad < 1)
                        modelo.Aguinaldo = (decimal)añosAntiguedad * (salarioBase / 2);
                    else if (añosAntiguedad < 3)
                        modelo.Aguinaldo = salarioBase / 2;
                    else if (añosAntiguedad < 10)
                        modelo.Aguinaldo = (salarioBase / 30) * 19;
                    else
                        modelo.Aguinaldo = (salarioBase / 30) * 21;
                }
            }
            else
            {
                modelo.Aguinaldo = 0;
            }

            // Bruto base + extras + reintegros
            modelo.SalarioBruto =
                (salarioDiario * modelo.DiasTrabajados) +
                modelo.HorasDiurnas +
                modelo.HorasNocturnas +
                modelo.Feriado +
                (modelo.Vacaciones ?? 0) +
                (modelo.Aguinaldo ?? 0) +
                (modelo.Indemnizacion ?? 0) +
                (modelo.Reintegro ?? 0);

            // Calcular monto cotizable para deducciones
            decimal montoCotizable = salarioBase;

            // Agregar vacaciones y bono vacaciones
            if (modelo.Vacaciones.HasValue && modelo.Vacaciones > 0)
            {
                montoCotizable += modelo.Vacaciones.Value;
            }

            // Incluir horas extra en el monto cotizable para empleados con más de 8 años
            if (añosAntiguedad > 8)
            {
                montoCotizable += (modelo.HorasDiurnas ?? 0) + (modelo.HorasNocturnas ?? 0);
            }

            // ISSS y AFP
            modelo.Isss = montoCotizable > 1000 ? 30 : montoCotizable * 0.03m;
            modelo.Afp = montoCotizable * 0.0725m;

            // Renta
            decimal? sobreElExceso = modelo.SalarioBruto - (modelo.Isss + modelo.Afp);
            if (sobreElExceso >= 0.01m && sobreElExceso <= 472)
                modelo.Renta = 0;
            else if (sobreElExceso >= 472.01m && sobreElExceso <= 895.24m)
                modelo.Renta = (sobreElExceso - 472) * 0.1m + 17.67m;
            else if (sobreElExceso >= 895.25m && sobreElExceso <= 2038.10m)
                modelo.Renta = (sobreElExceso - 895.24m) * 0.2m + 60;
            else if (sobreElExceso >= 2038.11m)
                modelo.Renta = (sobreElExceso - 2038.10m) * 0.3m + 288;


            else if (sobreElExceso >= 2038.11m)
                modelo.Renta = (sobreElExceso - 2038.10m) * 0.3m + 288.57m;

            // Salario Neto
            modelo.SalarioNeto = modelo.SalarioBruto - modelo.Isss - modelo.Afp - modelo.Renta;
        }
    }
}