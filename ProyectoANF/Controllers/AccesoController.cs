using Microsoft.AspNetCore.Mvc;
//using ProyectoANF.Data;
using ProyectoANF.Models;
using ProyectoANF.Controllers;
using ProyectoANF.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoANF.Controllers
{
   public class AccesoController : Controller{

      private readonly PlanillaPagoDbContext _context;

      public AccesoController(PlanillaPagoDbContext context){

         _context = context;
      }

      [HttpGet]
      public IActionResult Registrarse(){
         return View();
      }

        [HttpPost]
        public async Task<IActionResult> Registrarse(UsuarioVM modelo)
        {
            if(modelo.Contraseña != modelo.ConfirmarContraseña)
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden";
                return View();
            }

            Usuario usuario = new Usuario
            {
                NombreUsuario = modelo.NombreUsuario,
                Contraseña = modelo.Contraseña,
                Rol = modelo.Rol,
                FechaRegistro = DateTime.Now
            };

            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            if(usuario.UsuarioId != 0)
            {
                return RedirectToAction("Login","Acceso");
            }
            ViewData["Mensaje"] = "No se pudo crear el usuario";

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM modelo)
        {
            Usuario? usuario_encontrado = await _context.Usuarios
                .Where(u =>
                    u.NombreUsuario == modelo.NombreUsuario &&
                    u.Contraseña == modelo.Contraseña
                ).FirstOrDefaultAsync();

            if(usuario_encontrado == null)
            {
                ViewData["Mensaje"] = "Error en usuario o contraseña";
                return View();
            }

            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, usuario_encontrado.UsuarioId.ToString()),
            };
            
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties properties = new AuthenticationProperties()
            {
                AllowRefresh = true,
                IsPersistent = false
            };

            await HttpContext.SignInAsync( //para guardar en la galleta
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                properties
                ); 

            return RedirectToAction("Index", "Home");
        }
    }

}
