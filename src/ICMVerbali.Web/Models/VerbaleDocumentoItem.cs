namespace ICMVerbali.Web.Models;

// DTO per il checkrow dello step 4 (documenti, sez. 4 PDF). APPL/CONF/Note
// + AltroDescrizione condizionale alla voce "Altro" del catalogo.
public sealed class VerbaleDocumentoItem
{
    public Guid CatalogoTipoDocumentoId { get; init; }
    public string Codice { get; init; } = string.Empty;
    public string Etichetta { get; init; } = string.Empty;
    public int Ordine { get; init; }

    public bool Applicabile { get; set; }
    public bool Conforme { get; set; }
    public string? Note { get; set; }
    public string? AltroDescrizione { get; set; }
}
