using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

// Logica di business sui FirmaToken (B.11): generazione di un nuovo token con
// scadenza calcolata dalle options + validazione (esistenza/scadenza/uso singolo).
// La consumazione (UPDATE UsatoUtc) avviene atomicamente dentro la transazione
// di VerbaleRepository.FirmaImpresaAsync e non e' esposta dal manager.
public interface IFirmaTokenManager
{
    // Pre-calcola i campi di un nuovo token da passare al repository all'interno
    // della transazione di FirmaCseAsync. NON tocca il DB.
    FirmaTokenSeed CalcolaProssimoToken();

    // Lookup + validazione: ritorna il token se utilizzabile, altrimenti lancia
    // FirmaTokenInvalidoException con motivo specifico (NonTrovato/Scaduto/GiaUsato).
    Task<FirmaToken> ValidaTokenAsync(Guid token, CancellationToken ct = default);
}

// Materiale pre-calcolato lato manager per l'INSERT del token che il repository
// fara' dentro la transazione di FirmaCseAsync.
public sealed record FirmaTokenSeed(
    Guid TokenId,
    Guid Token,
    DateTime ScadenzaUtc,
    DateTime CreatedAt);
