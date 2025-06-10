using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoANF.Models;
namespace ProyectoANF.Controllers
{
    public class TrabajadoresController : Controller
    {
        private readonly PlanillaPagoDbContext _context;

        public TrabajadoresController(PlanillaPagoDbContext context)
        {
          _context = context;
        }

        public async Task<IActionResult> Index(int? pageNumber)
        {
            int pageSize = 5; // Mismo tamaño de página que en Planillas
            var trabajadores = _context.Trabajadores
                .OrderBy(t => t.Nombre); // Ordenamos por nombre, puedes cambiar el criterio

            return View(await PaginatedList<Trabajadore>.CreateAsync(trabajadores.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [HttpGet]
        public IActionResult AgregarTrabajador()
        {
            return View();
        }




        [HttpPost]
        public async Task<IActionResult> AgregarTrabajador(Trabajadore modelo)
        {
            if (ModelState.IsValid)
            {
                // Verificar si ya existe un trabajador con este NIT
                var trabajadorExistente = await _context.Trabajadores.FirstOrDefaultAsync(t => t.Nit == modelo.Nit);
                if (trabajadorExistente != null)
                {
                    ViewData["Mensaje"] = "Ya existe un trabajador con este NIT registrado en el sistema.";
                    return View(modelo);
                }

                // Continuar con la inserción normal si no existe
                Trabajadore empleado = new Trabajadore
                {
                    Nombre = modelo.Nombre,
                    Correo = modelo.Correo,
                    Dui = modelo.Dui,
                    Nit = modelo.Nit,
                    Afp = modelo.Nit,
                    Isss = modelo.Isss,
                    Cargo = modelo.Cargo,
                    SalarioBase = modelo.SalarioBase,
                    FechaContratacion = modelo.FechaContratacion,
                    Activo = modelo.Activo
                };
                await _context.Trabajadores.AddAsync(empleado);
                await _context.SaveChangesAsync();
                ViewData["Completado"] = "Empleado registrado con éxito";
                return View();
            }
            else
            {
                ViewData["Mensaje"] = "No pudo registrarse el empleado";
                return View();
            }
        }










        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Trabajadore empleado = await _context.Trabajadores.FirstAsync(e=>e.TrabajadorId == id);
            return View(empleado);
        }
        [HttpPost]
        public async Task<IActionResult> Editar(Trabajadore empleado)
        {
            _context.Trabajadores.Update(empleado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            Trabajadore empleado = await _context.Trabajadores.FirstAsync(e => e.TrabajadorId == id);
            return View(empleado);
        }
        [HttpPost]
        public async Task<IActionResult> Eliminar(Trabajadore empleado)
        {
            _context.Trabajadores.Remove(empleado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
