using iText.Kernel.Pdf;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoANF.Models;
using ProyectoANF.Models.ViewModels;
using System.Numerics;
using Rotativa.AspNetCore;



namespace ProyectoANF.Controllers
{
    public class PlanillaController : Controller
    {
        private readonly PlanillaPagoDbContext _context;
        private readonly ILogger<PlanillaController> _logger;


        public PlanillaController(PlanillaPagoDbContext context, ILogger<PlanillaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> ListaPlanillas(int? pageNumber)
        {
            int pageSize = 5;
            var planillas = _context.Planillas
            .Include(p => p.Trabajador)
            .OrderByDescending(p => p.FechaGeneracion);
            
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
            if(ModelState.IsValid)
            {
                try
                {
                    var trabajador = await _context.Trabajadores.FirstOrDefaultAsync(t=>t.TrabajadorId == modelo.TrabajadorId);
                    if(trabajador == null)
                    {
                        ViewData["Error"] = "El trabajador seleccionado no existe";
                        return View(modelo);
                    }
                    decimal salarioDiario = trabajador.SalarioBase / 30;
                    decimal salarioHora = salarioDiario / 8;

                    modelo.HorasDiurnas = modelo.HorasDiurnasCantidad *(salarioHora*2);
                    modelo.HorasNocturnas = modelo.HorasNocturnasCantidad *(salarioHora*2 + salarioHora*0.025m);
                    
                    modelo.SalarioBruto = (salarioDiario * modelo.DiasTrabajados) + modelo.HorasDiurnas + modelo.HorasNocturnas;

                    if (modelo.SalarioBruto > 1000)
                    {
                        modelo.Isss = 30;
                    }
                    else
                    {
                        modelo.Isss = modelo.SalarioBruto * 0.03m;
                    }
                    modelo.Afp = modelo.SalarioBruto * 0.0725m;
                    decimal? sobreElExceso = modelo.SalarioBruto-(modelo.Isss + modelo.Afp);

                    if(sobreElExceso>=0.01m && sobreElExceso <= 472)
                    {
                        modelo.Renta = 0;
                    }
                    if (sobreElExceso >= 472.01m && sobreElExceso <= 895.24m)
                    {
                        modelo.Renta = (sobreElExceso-472)*0.1m + 17.67m;
                    }
                    else if(sobreElExceso>=895.25m && sobreElExceso <= 2038.10m)
                    {
                        modelo.Renta = (sobreElExceso-895.24m)*0.2m + 60;
                    }
                    else if (sobreElExceso >= 2038.11m)
                    {
                        modelo.Renta = (sobreElExceso * 0.3m) + 288.57m;
                    }
                 
                    modelo.SalarioNeto = modelo.SalarioBruto - modelo.Isss - modelo.Afp - modelo.Renta;
                    modelo.FechaGeneracion = DateTime.Now;

                    await _context.Planillas.AddAsync(modelo);
                    await _context.SaveChangesAsync();

                    ViewData["Completado"] = "Planilla registrada con exito";
                    return RedirectToAction("Index","Home");
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

                    var trabajador = await _context.Trabajadores.FirstOrDefaultAsync(t => t.TrabajadorId == planillaExistente.TrabajadorId);
                    if (trabajador == null)
                    {
                        ViewData["Error"] = "El trabajador asociado a esta planilla no existe.";
                        return View(modelo);
                    }

                    decimal salarioDiario = trabajador.SalarioBase / 30;
                    decimal salarioHora = salarioDiario / 8;

                    // Calcular horas diurnas y nocturnas basadas en las cantidades actualizadas
                    planillaExistente.HorasDiurnas = modelo.HorasDiurnasCantidad * (salarioHora * 2);
                    planillaExistente.HorasNocturnas = modelo.HorasNocturnasCantidad * (salarioHora * 2 + salarioHora * 0.025m);

                    // Calcular salario bruto con los nuevos datos
                    planillaExistente.SalarioBruto =
                        (salarioDiario * modelo.DiasTrabajados) +
                        planillaExistente.HorasDiurnas +
                        planillaExistente.HorasNocturnas;

                    // Recalcular ISSS, AFP, y Renta
                    planillaExistente.Isss = planillaExistente.SalarioBruto > 1000 ? 30 : planillaExistente.SalarioBruto * 0.03m;
                    planillaExistente.Afp = planillaExistente.SalarioBruto * 0.0725m;

                    decimal? sobreElExceso = planillaExistente.SalarioBruto - (planillaExistente.Isss + planillaExistente.Afp);

                    if (sobreElExceso >= 0.01m && sobreElExceso <= 472)
                    {
                        planillaExistente.Renta = 0;
                    }
                    else if (sobreElExceso >= 472.01m && sobreElExceso <= 895.24m)
                    {
                        planillaExistente.Renta = (sobreElExceso - 472) * 0.1m + 17.67m;
                    }
                    else if (sobreElExceso >= 895.25m && sobreElExceso <= 2038.10m)
                    {
                        planillaExistente.Renta = (sobreElExceso - 895.24m) * 0.2m + 60;
                    }
                    else if (sobreElExceso >= 2038.11m)
                    {
                        planillaExistente.Renta = (sobreElExceso - 2038.10m) * 0.3m + 288.57m;
                    }

                    // Calcular salario neto
                    planillaExistente.SalarioNeto = planillaExistente.SalarioBruto - planillaExistente.Isss - planillaExistente.Afp - planillaExistente.Renta;

                    // Actualizar otros datos de la planilla
                    planillaExistente.Mes = modelo.Mes;
                    planillaExistente.Año = modelo.Año;
                    planillaExistente.DiasTrabajados = modelo.DiasTrabajados;
                    planillaExistente.Vacaciones = modelo.Vacaciones;
                    planillaExistente.Indemnizacion = modelo.Indemnizacion;
                    planillaExistente.Aguinaldo = modelo.Aguinaldo;

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Home");
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
    }
}
