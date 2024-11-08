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

        public async Task<IActionResult> Index()
        {
            List<Trabajadore> lista = await _context.Trabajadores.ToListAsync();
            
            return View(lista);
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
                Trabajadore empleado = new Trabajadore
                {
                    Nombre = modelo.Nombre,
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
                ViewData["Completado"] = "Empleado registrado con exito";
                return View();
            }
            else
            {
                ViewData["Mensaje"] = "No pudo registrarse el empleado";
                return View();
            }
            
        }
    }
}
