namespace ICMVerbali.Web.Models;

// DTO per il checkrow dello step 6 (condizioni ambientali, sez. 6 PDF).
// CONF e NonConforme sono mutuamente esclusivi (vincolo CK in DB);
// la UI applica la regola disabilitando l'altro al check.
public sealed class VerbaleCondizioneAmbientaleItem
{
    public Guid CatalogoTipoCondizioneAmbientaleId { get; init; }
    public string Codice { get; init; } = string.Empty;
    public string Etichetta { get; init; } = string.Empty;
    public int Ordine { get; init; }

    public bool Conforme { get; set; }
    public bool NonConforme { get; set; }
    public string? Note { get; set; }
}
