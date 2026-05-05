namespace ICMVerbali.Web.Entities;

public sealed class Cantiere
{
    public Guid Id { get; set; }
    public string Ubicazione { get; set; } = string.Empty;
    public string Tipologia { get; set; } = string.Empty;
    public decimal? ImportoAppalto { get; set; }
    public bool IsAttivo { get; set; }
}
