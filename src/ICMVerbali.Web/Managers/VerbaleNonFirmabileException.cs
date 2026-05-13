namespace ICMVerbali.Web.Managers;

// Lanciata da VerbaleManager.FirmaCseAsync quando la validazione hard fallisce.
// Contiene l'elenco puntuale degli errori cosi' la UI puo' mostrarli all'utente.
public sealed class VerbaleNonFirmabileException : Exception
{
    public IReadOnlyList<string> Errori { get; }

    public VerbaleNonFirmabileException(IReadOnlyList<string> errori)
        : base("Il verbale non puo' essere firmato: validazione fallita.")
    {
        Errori = errori;
    }
}
