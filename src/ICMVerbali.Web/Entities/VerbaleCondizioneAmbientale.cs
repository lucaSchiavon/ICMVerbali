namespace ICMVerbali.Web.Entities;

// Sez. 6 del PDF: condizioni ambientali (Illuminazione/Polveri/Rumore/Pulizia
// Strade). A differenza delle altre sezioni le colonne del PDF sono CONF e NC,
// non APPL e CONF: applico vincolo logico Conforme XOR NonConforme nel Manager.
public sealed class VerbaleCondizioneAmbientale
{
    public Guid VerbaleId { get; set; }
    public Guid CatalogoTipoCondizioneAmbientaleId { get; set; }
    public bool Conforme { get; set; }
    public bool NonConforme { get; set; }
    public string? Note { get; set; }
}
