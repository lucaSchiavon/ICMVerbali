namespace ICMVerbali.Web.Storage;

// Configurazione del magic-link per la firma Impresa (B.11).
// Bindato dalla sezione "FirmaToken" di appsettings.json.
public sealed class FirmaTokenOptions
{
    public const string SectionName = "FirmaToken";

    // Ore di validita' di un token dalla creazione. Default 48h (vedi
    // docs/01-design.md Addendum 2026-05-14). Override per ambiente:
    // override per cantieri con SLA piu' stretto o piu' rilassato.
    public int ScadenzaOreDefault { get; set; } = 48;
}
