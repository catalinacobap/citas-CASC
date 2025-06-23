using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitasEnfermeria.Models;
using Microsoft.EntityFrameworkCore;

namespace CitasEnfermeria.Pages
{
    public class AgendarEmergenciaModel : PageModel
    {
        private readonly EnfermeriaContext _context;

        public AgendarEmergenciaModel(EnfermeriaContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int IdEstudianteSeleccionado { get; set; }

        public List<EnfPersona> Estudiantes { get; set; } = new();

        public async Task OnGetAsync()
        {
            Estudiantes = await _context.EnfPersonas
                .Where(p => p.Tipo == "Estudiante")
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var estudiante = await _context.EnfPersonas.FindAsync(IdEstudianteSeleccionado);
            if (estudiante == null)
            {
                ModelState.AddModelError("", "Estudiante no encontrado.");
                return Page();
            }

            var citaEmergencia = new EnfCita
            {
                IdPersona = estudiante.Id,
                IdHorario = 1, // ⚠️ temporal, necesitás un horario dummy creado
                Estado = "Creada",
                HoraLlegada = TimeOnly.FromDateTime(DateTime.Now),
                UsuarioCreacion = "profe1"
            };

            _context.EnfCitas.Add(citaEmergencia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"✅ Cita de emergencia creada para {estudiante.Nombre}";
            return RedirectToPage("AgendarEmergencia");
        }
    }
}
