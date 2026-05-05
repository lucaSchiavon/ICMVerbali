using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Entities;

// Pagina 5 del PDF: firma CSE e firma Impresa Appaltatrice. Una sola firma per
// (Verbale, Tipo) — UNIQUE constraint a livello DB. ImmagineFirmaPath punta al
// PNG salvato dal signature pad (vedi docs/01-design.md §9.8).
public sealed class Firma
{
    public Guid Id { get; set; }
    public Guid VerbaleId { get; set; }
    public TipoFirmatario Tipo { get; set; }
    public string NomeFirmatario { get; set; } = string.Empty;
    public DateOnly DataFirma { get; set; }
    public string? ImmagineFirmaPath { get; set; }
}
