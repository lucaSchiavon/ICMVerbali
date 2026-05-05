namespace ICMVerbali.Web.Entities;

// Sez. 3 del PDF: 16 checkbox di attivita' in corso. Una riga per ogni voce
// del CatalogoTipoAttivita selezionata sul verbale. PK composta (VerbaleId,
// CatalogoTipoAttivitaId).
public sealed class VerbaleAttivita
{
    public Guid VerbaleId { get; set; }
    public Guid CatalogoTipoAttivitaId { get; set; }
    public bool Selezionato { get; set; }

    // Free text usato solo quando la voce di catalogo e' "Altro".
    public string? AltroDescrizione { get; set; }
}
