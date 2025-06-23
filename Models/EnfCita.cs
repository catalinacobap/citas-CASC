using System;
using System.Collections.Generic;

namespace CitasEnfermeria.Models;

public partial class EnfCita
{
    public int Id { get; set; }

    public int IdPersona { get; set; }

    public int IdHorario { get; set; }

    public TimeOnly? HoraLlegada { get; set; }

    public TimeOnly? HoraSalida { get; set; }

    public int? IdProfeLlegada { get; set; }

    public int? IdProfeSalida { get; set; }

    public string? MensajeLlegada { get; set; }

    public string? MensajeSalida { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreacion { get; set; } = null!;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public virtual EnfHorario IdHorarioNavigation { get; set; } = null!;

    public virtual EnfPersona IdPersonaNavigation { get; set; } = null!;

    public virtual EnfPersona? IdProfeLlegadaNavigation { get; set; }

    public virtual EnfPersona? IdProfeSalidaNavigation { get; set; }
}
