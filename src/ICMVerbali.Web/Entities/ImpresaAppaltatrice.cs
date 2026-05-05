namespace ICMVerbali.Web.Entities;

public sealed class ImpresaAppaltatrice
{
    public Guid Id { get; set; }
    public string RagioneSociale { get; set; } = string.Empty;
    public string? Indirizzo { get; set; }
    public string? CodiceFiscale { get; set; }
    public string? PartitaIva { get; set; }
    public string? NumeroIscrizioneRegistroImprese { get; set; }
    public bool IsAttivo { get; set; }
}
