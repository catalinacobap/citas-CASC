using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitasEnfermeria.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CitasEnfermeria.Pages
{
    public class AgendarCitasAdminModel : PageModel
    {
        private readonly EnfermeriaContext _context;
        public AgendarCitasAdminModel(EnfermeriaContext context)
        {
            _context = context;
        }

        // Elimina la variable hardcodeada UsuarioActual
        public string UsuarioActual { get; set; }

        private void SetUserType()
        {
            var persona = _context.EnfPersonas.FirstOrDefault(p => p.Usuario == UsuarioActual);
            EsProfesor = persona?.Tipo == "Profesor";
        }

        private void LoadData()
        {
            var persona = _context.EnfPersonas.FirstOrDefault(p => p.Usuario == UsuarioActual);
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
            HorariosDisponibles = query.OrderBy(h => h.Fecha).ThenBy(h => h.Hora).ToList();
            Estudiantes = _context.EnfPersonas.Where(p => p.Tipo == "Estudiante").OrderBy(p => p.Nombre).ToList();
        }

        private async Task LoadDataAsync()
        {
            var persona = await _context.EnfPersonas.FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);
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
            Estudiantes = await _context.EnfPersonas.Where(p => p.Tipo == "Estudiante").OrderBy(p => p.Nombre).ToListAsync();
        }

        public async Task OnGetAsync(string usuario)
        {
            UsuarioActual = usuario;
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
            Estudiantes = await _context.EnfPersonas.Where(p => p.Tipo == "Estudiante").OrderBy(p => p.Nombre).ToListAsync();
        }

        [BindProperty]
        public int HorarioSeleccionadoId { get; set; }
        public List<EnfHorario> HorariosDisponibles { get; set; } = new();
        public string? ErrorCita { get; set; }
        public bool EsProfesor { get; set; }
        public List<EnfPersona> Estudiantes { get; set; } = new();
        [BindProperty]
        public int IdEstudianteEmergencia { get; set; }

        public async Task<IActionResult> OnPostAsync(string usuario)
        {
            UsuarioActual = usuario;
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
            Estudiantes = await _context.EnfPersonas.Where(p => p.Tipo == "Estudiante").OrderBy(p => p.Nombre).ToListAsync();
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
            return RedirectToPage("AgendarCitasAdmin", new { usuario = UsuarioActual });
        }

        public class EmergenciaRequest { public int IdEstudiante { get; set; } }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAgendarEmergenciaAsync(string usuario)
        {
            UsuarioActual = usuario;
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var data = System.Text.Json.JsonSerializer.Deserialize<EmergenciaRequest>(body);
            if (data == null)
                return new JsonResult(new { success = false, message = "Datos inválidos: " + body });
            var persona = await _context.EnfPersonas.FirstOrDefaultAsync(p => p.Usuario == UsuarioActual);
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
                    Hora = new TimeOnly(0, 0), // 00:00
                    Estado = "Emergencia",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreacion = UsuarioActual
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
                Estado = "Emergencia",
                HoraLlegada = TimeOnly.FromDateTime(DateTime.Now),
                UsuarioCreacion = UsuarioActual
            };
            _context.EnfCitas.Add(citaEmergencia);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true, message = $"✅ Cita de emergencia creada para {estudiante.Nombre}" });
        }
    }
} 