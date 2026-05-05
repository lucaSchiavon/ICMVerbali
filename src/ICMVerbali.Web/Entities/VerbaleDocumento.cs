namespace ICMVerbali.Web.Entities;

// Sez. 4 del PDF: tabella verifica documentazione (art. 90 D.Lgs. 81/2008).
// Per ogni voce di catalogo: APPL (Applicabile) + CONF (Conforme) + Note.
public sealed class VerbaleDocumento
{
    public Guid VerbaleId { get; set; }
    public Guid CatalogoTipoDocumentoId { get; set; }
    public bool Applicabile { get; set; }
    public bool Conforme { get; set; }
    public string? Note { get; set; }

    // Free text usato solo quando la voce di catalogo e' "Altro".
    public string? AltroDescrizione { get; set; }
}
