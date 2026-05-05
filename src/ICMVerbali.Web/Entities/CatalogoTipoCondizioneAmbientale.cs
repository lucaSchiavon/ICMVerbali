namespace ICMVerbali.Web.Entities;

// Catalogo delle condizioni ambientali (sez. 6 del PDF: Illuminazione, Polveri,
// Rumore, Pulizia Strade). A differenza delle altre tre sezioni qui le colonne
// del PDF sono CONF/NC invece di APPL/CONF (vedi VerbaleCondizioneAmbientale).
public sealed class CatalogoTipoCondizioneAmbientale
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Etichetta { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool IsAttivo { get; set; }
}
