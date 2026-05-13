using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Models;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository dell'aggregate root Verbale. Le scritture passano per
// CreateBozzaWithChildrenAsync che inserisce in transazione il record principale,
// le figlie checklist pre-popolate e la riga di audit "Creazione" (§9.12).
// Le letture per la Home usano DTO joinati (VerbaleListItem) per evitare N+1.
public interface IVerbaleRepository
{
    // Crea bozza completa: Verbale + N righe in VerbaleAttivita / VerbaleDocumento /
    // VerbaleApprestamento / VerbaleCondizioneAmbientale + 1 riga in VerbaleAudit.
    // Tutto in una transazione: in caso di errore nessuna riga residua.
    Task CreateBozzaWithChildrenAsync(
        Verbale verbale,
        IEnumerable<VerbaleAttivita> attivita,
        IEnumerable<VerbaleDocumento> documenti,
        IEnumerable<VerbaleApprestamento> apprestamenti,
        IEnumerable<VerbaleCondizioneAmbientale> condizioniAmbientali,
        VerbaleAudit audit,
        CancellationToken ct = default);

    Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Aggiorna i campi della sezione 1 (anagrafica). UpdatedAt rifissato a SYSUTCDATETIME().
    // Pensato per lo step 1 del wizard quando l'utente modifica una bozza.
    Task UpdateAnagraficaAsync(
        Guid id,
        DateOnly data,
        Guid cantiereId,
        Guid committenteId,
        Guid impresaAppaltatriceId,
        Guid rlPersonaId,
        Guid cspPersonaId,
        Guid csePersonaId,
        Guid dlPersonaId,
        CancellationToken ct = default);

    // Step 2 wizard: esito complessivo + condizioni meteo + temperatura.
    // I campi sono nullable in DB per ammettere bozze parziali.
    Task UpdateMeteoEsitoAsync(
        Guid id,
        Entities.Enums.EsitoVerifica? esito,
        Entities.Enums.CondizioneMeteo? meteo,
        int? temperaturaCelsius,
        CancellationToken ct = default);

    // Step 7 wizard: gestione interferenze (Sez. 7 PDF) + note libere.
    Task UpdateInterferenzeAsync(
        Guid id,
        Entities.Enums.GestioneInterferenze? interferenze,
        string? interferenzeNote,
        CancellationToken ct = default);

    // -------- checklist (step 3-6 wizard) --------------------------------
    // Letture joinate con il catalogo: includono le righe anche se la voce di
    // catalogo e' stata disattivata dopo la creazione (snapshot del verbale).
    Task<IReadOnlyList<VerbaleAttivitaItem>>
        GetAttivitaByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    Task<IReadOnlyList<VerbaleDocumentoItem>>
        GetDocumentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    Task<IReadOnlyList<VerbaleApprestamentoItem>>
        GetApprestamentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    Task<IReadOnlyList<VerbaleCondizioneAmbientaleItem>>
        GetCondizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    // Aggiornamenti bulk: una transazione per checklist, UPDATE multipla.
    // Bumpa UpdatedAt sul Verbale dentro la stessa transazione.
    Task UpdateAttivitaBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleAttivita> rows, CancellationToken ct = default);

    Task UpdateDocumentiBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleDocumento> rows, CancellationToken ct = default);

    Task UpdateApprestamentiBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleApprestamento> rows, CancellationToken ct = default);

    Task UpdateCondizioniBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleCondizioneAmbientale> rows, CancellationToken ct = default);

    // -------- prescrizioni CSE (step 8 wizard) --------------------------
    // Lista in ordine crescente di Ordine.
    Task<IReadOnlyList<PrescrizioneCse>>
        GetPrescrizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    // Sostituzione completa: una transazione con DELETE delle righe esistenti,
    // INSERT delle nuove, bump UpdatedAt sul Verbale. Pragmatica per liste piccole
    // (tipicamente < 20 prescrizioni per verbale): evita la complessita' di un diff
    // INSERT/UPDATE/DELETE selettivo.
    Task ReplacePrescrizioniAsync(
        Guid verbaleId, IEnumerable<PrescrizioneCse> rows, CancellationToken ct = default);

    // -------- liste Home -------------------------------------------------
    // Verbali del giorno (esclude bozze e soft-deleted), ordinati per UpdatedAt DESC.
    Task<IReadOnlyList<VerbaleListItem>> GetByDataAsync(DateOnly data, CancellationToken ct = default);

    // Bozze attive (Stato = 0, non eliminate), ordinate per UpdatedAt DESC.
    Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default);

    // -------- firma (transizione Bozza -> FirmatoCse) -------------------
    // Operazione composta in UNA transazione (B.11 estende B.10 con il token):
    //   1. Lock + verifica Stato == Bozza (idempotenza: due click sul bottone non firmano due volte)
    //   2. Calcola Numero come MAX(Numero)+1 per l'Anno dato (UNIQUE filtrato gestisce race)
    //   3. INSERT in Firma (PK verbaleId+Tipo)
    //   4. UPDATE Verbale: Stato=FirmatoCse, Numero, Anno, UpdatedAt
    //   5. INSERT in VerbaleAudit (EventoTipo=Firma)
    //   6. INSERT in FirmaToken (atomico con la firma: nessun verbale FirmatoCse
    //      esiste senza il suo magic-link per la firma Impresa).
    // Restituisce il (Numero, Anno, TokenImpresa) assegnato. Il filesystem
    // (PNG firma) NON e' toccato qui — il chiamante deve averlo gia' salvato
    // prima di chiamare.
    Task<FirmaCseResult> FirmaCseAsync(
        Guid verbaleId,
        int anno,
        string nomeFirmatario,
        DateOnly dataFirma,
        string immagineFirmaPath,
        Guid utenteId,
        FirmaTokenInputs tokenImpresa,
        CancellationToken ct = default);

    // -------- firma (transizione FirmatoCse -> FirmatoImpresa) ---------
    // Operazione composta in UNA transazione (B.11):
    //   1. Lock + verifica Stato == FirmatoCse (idempotenza)
    //   2. INSERT in Firma con Tipo=ImpresaAppaltatrice
    //   3. UPDATE Verbale: Stato=FirmatoImpresa, UpdatedAt (Numero/Anno NON cambiano)
    //   4. INSERT in VerbaleAudit (EventoTipo=Firma)
    //   5. UPDATE FirmaToken SET UsatoUtc = SYSUTCDATETIME() WHERE Id = @TokenId
    //      AND UsatoUtc IS NULL (uso singolo: rejecta race tra due tab aperte)
    // Niente assegnazione Numero/Anno: sono gia' stati assegnati alla firma CSE.
    // UtenteId per l'audit e' tipicamente CompilatoDaUtenteId del verbale (l'impresa
    // non ha un account: vedi docs/01-design.md Addendum 2026-05-14 §7).
    Task FirmaImpresaAsync(
        Guid verbaleId,
        Guid tokenId,
        string nomeFirmatario,
        DateOnly dataFirma,
        string immagineFirmaPath,
        Guid utenteId,
        CancellationToken ct = default);
}

// Risultato della firma CSE. TokenImpresa e' il GUID da mettere nell'URL del
// magic-link condiviso con l'Impresa (es. /firma-impresa/{TokenImpresa}).
public sealed record FirmaCseResult(int NumeroAssegnato, int Anno, Guid TokenImpresa);

// Materiale del token impresa che il chiamante (manager) pre-calcola e passa al
// repository per l'INSERT atomico con la firma CSE.
public sealed record FirmaTokenInputs(
    Guid TokenId,
    Guid Token,
    DateTime ScadenzaUtc,
    DateTime CreatedAt);
