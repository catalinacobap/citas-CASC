using System;
using System.Collections.Generic;

namespace CitasEnfermeria.Models;

public partial class EnfHorario
{
    public int Id { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreacion { get; set; } = null!;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public virtual ICollection<EnfCita> EnfCita { get; set; } = new List<EnfCita>();
}
