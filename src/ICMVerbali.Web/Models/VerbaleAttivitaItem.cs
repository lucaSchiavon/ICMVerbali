namespace ICMVerbali.Web.Models;

// DTO per il checkrow dello step 3 del wizard. Combina la riga di
// VerbaleAttivita (per il verbale corrente) con l'etichetta del catalogo,
// cosi' la UI ha tutto quello che le serve in una sola lista.
//
// Campi catalogo: init-only (mai cambiati dalla UI).
// Campi verbale: set (la UI muta i flag al toggle del checkbox).
public sealed class VerbaleAttivitaItem
{
    public Guid CatalogoTipoAttivitaId { get; init; }
    public string Codice { get; init; } = string.Empty;
    public string Etichetta { get; init; } = string.Empty;
    public int Ordine { get; init; }

    public bool Selezionato { get; set; }
    public string? AltroDescrizione { get; set; }
}
