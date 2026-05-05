namespace ICMVerbali.Web.Entities;

// Catalogo delle attivita' in corso (sez. 3 del PDF). Codice immutabile,
// IsAttivo per disattivare voci obsolete senza hard delete.
public sealed class CatalogoTipoAttivita
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Etichetta { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool IsAttivo { get; set; }
}
