namespace ICMVerbali.Web.Entities;

// Sez. 1 del PDF. Una presenza e' associata a una Persona in anagrafica
// (PersonaId) oppure compilata a mano (NominativoLibero/ImpresaLibera).
// Vincolo logico: PersonaId IS NOT NULL OR NominativoLibero IS NOT NULL,
// applicato dal Manager.
public sealed class Presenza
{
    public Guid Id { get; set; }
    public Guid VerbaleId { get; set; }

    public Guid? PersonaId { get; set; }
    public string? NominativoLibero { get; set; }
    public string? ImpresaLibera { get; set; }

    public int Ordine { get; set; }
}
