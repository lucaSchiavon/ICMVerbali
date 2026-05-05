namespace ICMVerbali.Web.Entities;

// Sez. 9 del PDF: documentazione fotografica. Il file binario sta sul filesystem
// (vedi docs/01-design.md §7), qui memorizziamo solo path relativo + metadati.
public sealed class Foto
{
    public Guid Id { get; set; }
    public Guid VerbaleId { get; set; }
    public string FilePathRelativo { get; set; } = string.Empty;
    public string? Didascalia { get; set; }
    public int Ordine { get; set; }
    public DateTime CreatedAt { get; set; }
}
