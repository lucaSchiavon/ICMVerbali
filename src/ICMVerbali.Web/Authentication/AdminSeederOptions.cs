namespace ICMVerbali.Web.Authentication;

// Configurazione del seed dell'utente admin iniziale.
// Username puo' stare in appsettings.json (non sensibile).
// Password DEVE arrivare da user-secrets (dev) o variabile d'ambiente
// ConnectionStrings__Default style: Admin__DefaultPassword (prod).
// Vedi docs/01-design.md §9.4 e CLAUDE.md "Gestione di segreti".
public sealed class AdminSeederOptions
{
    public const string SectionName = "Admin";

    public string DefaultUsername { get; set; } = "admin";
    public string? DefaultPassword { get; set; }
}
