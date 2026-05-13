namespace ICMVerbali.Web.Managers;

// Eccezione lanciata quando un token magic-link per la firma impresa non e'
// utilizzabile. La pagina /firma-impresa/{token} la cattura per scegliere il
// messaggio da mostrare in base al Motivo. Vedi docs/01-design.md Addendum 2026-05-14.
public sealed class FirmaTokenInvalidoException : Exception
{
    public FirmaTokenInvalidoMotivo Motivo { get; }

    public FirmaTokenInvalidoException(FirmaTokenInvalidoMotivo motivo, string message)
        : base(message)
    {
        Motivo = motivo;
    }
}

public enum FirmaTokenInvalidoMotivo : byte
{
    NonTrovato = 0,
    Scaduto = 1,
    GiaUsato = 2,
}
