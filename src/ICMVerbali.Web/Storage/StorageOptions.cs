namespace ICMVerbali.Web.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    // Path base per gli upload (foto verbali, firme). Puo' essere relativo al
    // ContentRoot dell'app o assoluto. NON deve coincidere con wwwroot/ ne' con
    // una sottocartella servita dal middleware static files: i file vanno serviti
    // tramite endpoint controllato con auth (vedi docs/01-design.md §7).
    public string UploadsBasePath { get; set; } = string.Empty;
}
