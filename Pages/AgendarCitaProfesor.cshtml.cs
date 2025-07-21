using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitasEnfermeria.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CitasEnfermeria.Pages
{
    public class AgendarCitaProfesorModel : PageModel
    {
        private readonly EnfermeriaContext _context;
        public AgendarCitaProfesorModel(EnfermeriaContext context)
        {
            _context = context;
        }

        private string UsuarioActual = "profe1"; // Cambiar por autenticación real

        [BindProperty]
        public int HorarioSeleccionadoId { get; set; }
        public List<EnfHorario> HorariosDisponibles { get; set; } = new();
        public string? ErrorCita { get; set; }
        public bool EsProfesor { get; set; }
        public List<EnfPersona> Estudiantes { get; set; } = new();

        public async Task OnGetAsync()
        {
            var persona = await _context.EnfPersonas.FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);
            EsProfesor = persona?.Tipo == "Profesor";
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            IQueryable<EnfHorario> query = _context.EnfHorarios.Where(h => h.Estado == "Disponible");
            if (persona != null && (persona.Tipo == "Funcionario" || persona.Tipo == "Profesor"))
            {
                query = query.Where(h => h.Fecha >= hoy);
            }
            else
            {
                query = query.Where(h => h.Fecha == hoy);
            }
            HorariosDisponibles = await query.OrderBy(h => h.Fecha).ThenBy(h => h.Hora).ToListAsync();
            // Para emergencia
            Estudiantes = await _context.EnfPersonas.Where(p => p.Tipo == "Estudiante").OrderBy(p => p.Nombre).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var persona = await _context.EnfPersonas.FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);
            if (persona == null)
            {
                ErrorCita = "Usuario no válido.";
                return Page();
            }
            var horario = await _context.EnfHorarios.FindAsync(HorarioSeleccionadoId);
            if (horario == null || horario.Estado != "Disponible")
            {
                ErrorCita = "El horario ya no está disponible.";
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
            return RedirectToPage("AgendarCitaProfesor");
        }
    }
} 