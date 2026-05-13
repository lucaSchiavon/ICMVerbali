using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers;

// Validazione "hard" applicata alla transizione Bozza -> FirmatoCse (vedi
// docs/01-design.md §9.22). Vincolo minimo decisorio (2026-05-13):
//   - tutte le 7 FK anagrafiche valorizzate (NOT NULL in DB, controllo difensivo)
//   - Esito + Meteo non null
// Checklist (3-6), prescrizioni (8), foto (9), temperatura, interferenze restano
// liberi: una bozza puo' essere firmata senza foto o senza prescrizioni.
public static class VerbaleValidator
{
    public static VerbaleValidationResult PuoFirmare(Verbale verbale)
    {
        ArgumentNullException.ThrowIfNull(verbale);

        var errori = new List<string>();

        if (verbale.CantiereId == Guid.Empty)
            errori.Add("Cantiere non impostato.");
        if (verbale.CommittenteId == Guid.Empty)
            errori.Add("Committente non impostato.");
        if (verbale.ImpresaAppaltatriceId == Guid.Empty)
            errori.Add("Impresa appaltatrice non impostata.");
        if (verbale.RlPersonaId == Guid.Empty)
            errori.Add("Responsabile dei lavori non impostato.");
        if (verbale.CspPersonaId == Guid.Empty)
            errori.Add("Coordinatore sicurezza progettazione non impostato.");
        if (verbale.CsePersonaId == Guid.Empty)
            errori.Add("Coordinatore sicurezza esecuzione non impostato.");
        if (verbale.DlPersonaId == Guid.Empty)
            errori.Add("Direttore lavori non impostato.");

        if (verbale.Esito is null)
            errori.Add("Esito della verifica non selezionato (sez. 2).");
        if (verbale.Meteo is null)
            errori.Add("Condizione meteo non selezionata (sez. 2).");

        return new VerbaleValidationResult(errori.Count == 0, errori);
    }
}

public sealed record VerbaleValidationResult(bool IsValid, IReadOnlyList<string> Errori);
