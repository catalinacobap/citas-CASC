using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitasEnfermeria.Models;
using Microsoft.EntityFrameworkCore;

namespace CitasEnfermeria.Pages
{
    [IgnoreAntiforgeryToken]
    public class AgendarCitaModel : PageModel
    {
        private readonly EnfermeriaContext _context;

        public AgendarCitaModel(EnfermeriaContext context)
        {
            _context = context;
        }

        public string UsuarioActual { get; set; }

        public bool EsProfesor { get; set; }

        [BindProperty]
        public int HorarioSeleccionadoId { get; set; }

        public List<EnfHorario> HorariosDisponibles { get; set; } = new();

        public List<EnfPersona> Estudiantes { get; set; } = new();

        public string? ErrorCita { get; set; } // Para pasar el error a la vista

        public async Task OnGetAsync(string usuario)
        {
            UsuarioActual = usuario;
            var persona = await _context.EnfPersonas
                .FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);

            EsProfesor = persona?.Tipo == "Profesor";

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

            if (EsProfesor)
            {
                Estudiantes = await _context.EnfPersonas
                    .Where(p => p.Tipo == "Estudiante")
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync(string usuario)
        {
            UsuarioActual = usuario;
            var persona = await _context.EnfPersonas
                .FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);

            EsProfesor = persona?.Tipo == "Profesor";

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

            // Validación: solo un estudiante puede tener una cita normal por día
            if (persona.Tipo == "Estudiante")
            {
                var fechaCita = horario.Fecha;
                var yaTieneCita = await _context.EnfCitas
                    .Include(c => c.IdHorarioNavigation)
                    .AnyAsync(c => c.IdPersona == persona.Id &&
                                  c.IdHorarioNavigation.Fecha == fechaCita &&
                                  c.Estado != "Cancelada");
                if (yaTieneCita)
                {
                    ErrorCita = "Ya tienes una cita agendada para este día. Solo puedes agendar una cita por día.";
                    return Page();
                }
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
            return RedirectToPage("AgendarCita", new { usuario = UsuarioActual });
        }

        public class EmergenciaRequest { public int IdEstudiante { get; set; } }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAgendarEmergenciaAsync()
        {
            try
            {
                var usuario = Request.Query["usuario"].ToString();
                if (string.IsNullOrEmpty(usuario))
                    return new JsonResult(new { success = false, message = "Usuario no especificado." });

                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<EmergenciaRequest>(body);
                if (data == null)
                    return new JsonResult(new { success = false, message = "Datos inválidos." });

                var persona = await _context.EnfPersonas.FirstOrDefaultAsync(p => p.Usuario == usuario);
                if (persona == null || persona.Tipo != "Profesor")
                    return new JsonResult(new { success = false, message = "Solo los profesores pueden agendar emergencias." });

                var estudiante = await _context.EnfPersonas.FindAsync(data.IdEstudiante);
                if (estudiante == null || estudiante.Tipo != "Estudiante")
                    return new JsonResult(new { success = false, message = "Estudiante no encontrado." });

                // Buscar o crear el horario de emergencia para hoy
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                var horarioEmergencia = await _context.EnfHorarios
                    .FirstOrDefaultAsync(h => h.Fecha == hoy && h.Estado == "Emergencia");

                if (horarioEmergencia == null)
                {
                    var nuevoHorario = new EnfHorario
                    {
                        Fecha = hoy,
                        Hora = new TimeOnly(0, 0),
                        Estado = "Emergencia",
                        FechaCreacion = DateTime.Now,
                        UsuarioCreacion = usuario
                    };
                    _context.EnfHorarios.Add(nuevoHorario);
                    await _context.SaveChangesAsync();
                    horarioEmergencia = nuevoHorario;
                }

                // Registrar la cita de emergencia
                var citaEmergencia = new EnfCita
                {
                    IdPersona = estudiante.Id,
                    IdHorario = horarioEmergencia.Id,
                    Estado = "Creada", // Valor permitido por la BD
                    HoraLlegada = TimeOnly.FromDateTime(DateTime.Now),
                    UsuarioCreacion = usuario
                };

                _context.EnfCitas.Add(citaEmergencia);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = $"✅ Cita de emergencia creada para {estudiante.Nombre}" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "";
                return new JsonResult(new { success = false, message = "Error al agendar emergencia: " + ex.Message + " " + inner });
            }
        }
    }
}
