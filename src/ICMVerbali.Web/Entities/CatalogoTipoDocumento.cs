namespace ICMVerbali.Web.Entities;

// Catalogo dei documenti verificati (sez. 4 del PDF, art. 90 D.Lgs. 81/2008).
public sealed class CatalogoTipoDocumento
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Etichetta { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool IsAttivo { get; set; }
}
