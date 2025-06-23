using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitasEnfermeria.Models;
using Microsoft.EntityFrameworkCore;

namespace CitasEnfermeria.Pages
{
    public class AgendarCitaModel : PageModel
    {
        private readonly EnfermeriaContext _context;

        public AgendarCitaModel(EnfermeriaContext context)
        {
            _context = context;
        }

        // 🔄 Cambiá este valor según el usuario que querés simular
        private string UsuarioActual = "func1"; // "profe1", "func1"

        [BindProperty]
        public int HorarioSeleccionadoId { get; set; }

        public List<EnfHorario> HorariosDisponibles { get; set; } = new();

        public async Task OnGetAsync()
        {
            var persona = await _context.EnfPersonas
                .FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);

            if (persona == null)
            {
                ModelState.AddModelError("", "Usuario no válido.");
                HorariosDisponibles = new();
                return;
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            IQueryable<EnfHorario> query = _context.EnfHorarios
                .Where(h => h.Estado == "Disponible");

            if (persona.Tipo == "Estudiante")
            {
                query = query.Where(h => h.Fecha == hoy);
            }
            else if (persona.Tipo == "Funcionario" || persona.Tipo == "Profesor")
            {
                query = query.Where(h => h.Fecha >= hoy);
            }

            HorariosDisponibles = await query
                .OrderBy(h => h.Fecha)
                .ThenBy(h => h.Hora)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var persona = await _context.EnfPersonas
                .FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);

            if (persona == null)
            {
                ModelState.AddModelError("", "Usuario no válido.");
                return Page();
            }

            var horario = await _context.EnfHorarios.FindAsync(HorarioSeleccionadoId);

            if (horario == null || horario.Estado != "Disponible")
            {
                ModelState.AddModelError("", "El horario ya no está disponible.");
                return Page();
            }

            var nuevaCita = new EnfCita
            {
                IdPersona = persona.Id,
                IdHorario = horario.Id,
                Estado = "Creada",
                UsuarioCreacion = UsuarioActual
            };

            _context.EnfCitas.Add(nuevaCita);

            horario.Estado = "Reservado";
            horario.UsuarioModificacion = UsuarioActual;
            horario.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Cita agendada correctamente.";
            return RedirectToPage("AgendarCita");
        }
    }
}
