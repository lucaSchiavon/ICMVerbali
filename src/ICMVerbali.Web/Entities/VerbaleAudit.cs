using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Entities;

// Audit log minimal delle transizioni di stato del Verbale. Niente diff dei
// contenuti (troppo pesante e poco utile). Vedi docs/01-design.md §9.12.
public sealed class VerbaleAudit
{
    public Guid Id { get; set; }
    public Guid VerbaleId { get; set; }
    public Guid UtenteId { get; set; }
    public DateTime DataEvento { get; set; }
    public EventoAuditTipo EventoTipo { get; set; }
    public string? Note { get; set; }
}
