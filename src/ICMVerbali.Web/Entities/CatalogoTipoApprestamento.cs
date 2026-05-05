using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Entities;

// Catalogo degli apprestamenti di sicurezza (sez. 5 del PDF, art. 92 D.Lgs. 81/2008).
// Le voci sono raggruppate in 4 sottosezioni 5.1/5.2/5.3/5.4.
public sealed class CatalogoTipoApprestamento
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Etichetta { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool IsAttivo { get; set; }
    public SottosezioneApprestamento Sottosezione { get; set; }
}
