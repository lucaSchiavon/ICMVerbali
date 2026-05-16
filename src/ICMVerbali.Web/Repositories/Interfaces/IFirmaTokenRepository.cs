using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository per la tabella FirmaToken (B.11). La CREAZIONE iniziale di un token
// avviene dentro la transazione di VerbaleRepository.FirmaCseAsync (atomico con
// la firma CSE). Qui esponiamo: read by token (pagina pubblica), read by verbale
// (debug/detail view), lookup ultimo attivo (B.12 detail view) e rigenerazione
// (B.12 azione del CSE).
public interface IFirmaTokenRepository
{
    // Lookup del token per la pagina /firma-impresa/{token}.
    Task<FirmaToken?> GetByTokenAsync(Guid token, CancellationToken ct = default);

    // Tutti i token emessi per un verbale (per debug / detail view / audit).
    Task<IReadOnlyList<FirmaToken>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    // B.12: ultimo token utilizzabile (UsatoUtc IS NULL AND RevocatoUtc IS NULL
    // AND ScadenzaUtc > SYSUTCDATETIME()) per un verbale, oppure null se nessuno
    // dei token emessi e' ancora valido. Usato dalla detail view per mostrare il
    // link copiabile o, in alternativa, un bottone "Genera nuovo link".
    Task<FirmaToken?> GetUltimoAttivoAsync(Guid verbaleId, CancellationToken ct = default);

    // B.12: in UNA transazione: marca tutti i token attivi del verbale come
    // RevocatoUtc = SYSUTCDATETIME(), inserisce un nuovo FirmaToken (seed
    // pre-calcolato dal manager) e registra l'evento in VerbaleAudit con
    // EventoTipo=RigenerazioneToken. Il chiamante deve aver gia' verificato che
    // il verbale sia in stato FirmatoCse: qui non c'e' lock sul Verbale per non
    // contendere con FirmaImpresaAsync — la difesa e' su MarkTokenUsato (filtra
    // RevocatoUtc IS NULL).
    Task RigeneraAsync(
        Guid verbaleId,
        FirmaTokenInputs nuovoToken,
        Guid utenteId,
        CancellationToken ct = default);
}
