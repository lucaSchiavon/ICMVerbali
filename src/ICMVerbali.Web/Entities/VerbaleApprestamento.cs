namespace ICMVerbali.Web.Entities;

// Sez. 5 del PDF: apprestamenti e sicurezza (art. 92 D.Lgs. 81/2008), 7 voci
// raggruppate in 4 sottosezioni 5.1-5.4. Stesso schema di VerbaleDocumento:
// APPL + CONF + Note.
public sealed class VerbaleApprestamento
{
    public Guid VerbaleId { get; set; }
    public Guid CatalogoTipoApprestamentoId { get; set; }
    public bool Applicabile { get; set; }
    public bool Conforme { get; set; }
    public string? Note { get; set; }
}
