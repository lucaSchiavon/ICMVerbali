using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

// Logica di business sui FirmaToken (B.11): generazione di un nuovo token con
// scadenza calcolata dalle options + validazione (esistenza/scadenza/uso singolo).
// La consumazione (UPDATE UsatoUtc) avviene atomicamente dentro la transazione
// di VerbaleRepository.FirmaImpresaAsync e non e' esposta dal manager.
//
// B.12: aggiunti GetLinkAttivoAsync e RigeneraAsync per consentire al CSE di
// recuperare/rigenerare il magic-link dalla detail view.
public interface IFirmaTokenManager
{
    // Pre-calcola i campi di un nuovo token da passare al repository all'interno
    // della transazione di FirmaCseAsync. NON tocca il DB.
    FirmaTokenSeed CalcolaProssimoToken();

    // Lookup + validazione: ritorna il token se utilizzabile, altrimenti lancia
    // FirmaTokenInvalidoException con motivo specifico (NonTrovato/Scaduto/GiaUsato/Revocato).
    Task<FirmaToken> ValidaTokenAsync(Guid token, CancellationToken ct = default);

    // B.12: ritorna l'ultimo token utilizzabile del verbale (se esiste) per
    // mostrare nella detail view il link copiabile senza obbligare l'utente
    // a rigenerarlo. Null se tutti i token sono scaduti/usati/revocati o se
    // non ce ne sono (verbale ancora in Bozza).
    Task<FirmaToken?> GetLinkAttivoAsync(Guid verbaleId, CancellationToken ct = default);

    // B.12: rigenera il magic-link impresa per un verbale FirmatoCse. Revoca
    // tutti i token attivi precedenti, ne crea uno nuovo, registra l'audit.
    // Lancia InvalidOperationException se il verbale non e' FirmatoCse (l'azione
    // non ha senso ne' su Bozza ne' su FirmatoImpresa). Restituisce il GUID del
    // nuovo token da mettere nell'URL.
    Task<Guid> RigeneraTokenAsync(
        Guid verbaleId,
        Guid utenteId,
        CancellationToken ct = default);
}

// Materiale pre-calcolato lato manager per l'INSERT del token che il repository
// fara' dentro la transazione di FirmaCseAsync.
public sealed record FirmaTokenSeed(
    Guid TokenId,
    Guid Token,
    DateTime ScadenzaUtc,
    DateTime CreatedAt);
