namespace ICMVerbali.Web.Entities;

// Sez. 8 del PDF: lista delle prescrizioni e osservazioni del CSE. Modellata
// come lista di item (non blob testo unico) per supportare in futuro il
// tracking accettazione/contestazione 24h previsto dalla nota legale di p.4
// (vedi docs/01-design.md §9.16, §9.9).
public sealed class PrescrizioneCse
{
    public Guid Id { get; set; }
    public Guid VerbaleId { get; set; }
    public string Testo { get; set; } = string.Empty;
    public int Ordine { get; set; }
}
