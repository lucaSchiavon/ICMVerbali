using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Models;

// DTO per il checkrow dello step 5 (apprestamenti, sez. 5 PDF). Stesso schema
// dei documenti (APPL/CONF/Note) ma con la SottosezioneApprestamento per
// raggruppare le voci nelle 4 sotto-tabelle 5.1-5.4.
public sealed class VerbaleApprestamentoItem
{
    public Guid CatalogoTipoApprestamentoId { get; init; }
    public string Codice { get; init; } = string.Empty;
    public string Etichetta { get; init; } = string.Empty;
    public int Ordine { get; init; }
    public SottosezioneApprestamento Sottosezione { get; init; }

    public bool Applicabile { get; set; }
    public bool Conforme { get; set; }
    public string? Note { get; set; }
}
