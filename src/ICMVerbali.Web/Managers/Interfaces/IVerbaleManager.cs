using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Models;

namespace ICMVerbali.Web.Managers.Interfaces;

// Manager dell'aggregate root Verbale. Orchestrazione della creazione bozza:
// fetch dei 4 cataloghi, generazione delle figlie checklist con flag a false,
// creazione transazionale via repository (vedi B.8a / docs/01-design.md §2.4).
public interface IVerbaleManager
{
    // Crea una bozza completa: record Verbale + figlie checklist pre-popolate
    // (una riga per ogni voce di catalogo attiva) + riga audit "Creazione".
    // Tutto in transazione: rollback automatico in caso di errore.
    Task<Verbale> CreaBozzaAsync(
        DateOnly data,
        Guid cantiereId,
        Guid committenteId,
        Guid impresaAppaltatriceId,
        Guid rlPersonaId,
        Guid cspPersonaId,
        Guid csePersonaId,
        Guid dlPersonaId,
        Guid compilatoDaUtenteId,
        CancellationToken ct = default);

    Task<Verbale?> GetAsync(Guid id, CancellationToken ct = default);

    // Step 1 wizard in modifica: aggiorna data + 7 FK anagrafica.
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

    // Step 2 wizard: esito + meteo + temperatura. I campi sono nullable perche'
    // in bozza l'utente puo' salvare parzialmente (validazione hard alla firma, §9.22).
    Task UpdateMeteoEsitoAsync(
        Guid id,
        EsitoVerifica? esito,
        CondizioneMeteo? meteo,
        int? temperaturaCelsius,
        CancellationToken ct = default);

    // Step 7 wizard: gestione interferenze (Sez. 7 PDF) + note libere.
    Task UpdateInterferenzeAsync(
        Guid id,
        GestioneInterferenze? interferenze,
        string? interferenzeNote,
        CancellationToken ct = default);

    // -------- checklist (step 3-6 wizard) --------------------------------
    Task<IReadOnlyList<VerbaleAttivitaItem>>
        GetAttivitaAsync(Guid verbaleId, CancellationToken ct = default);

    Task<IReadOnlyList<VerbaleDocumentoItem>>
        GetDocumentiAsync(Guid verbaleId, CancellationToken ct = default);

    Task<IReadOnlyList<VerbaleApprestamentoItem>>
        GetApprestamentiAsync(Guid verbaleId, CancellationToken ct = default);

    Task<IReadOnlyList<VerbaleCondizioneAmbientaleItem>>
        GetCondizioniAsync(Guid verbaleId, CancellationToken ct = default);

    Task UpdateAttivitaAsync(
        Guid verbaleId, IEnumerable<Entities.VerbaleAttivita> rows, CancellationToken ct = default);

    Task UpdateDocumentiAsync(
        Guid verbaleId, IEnumerable<Entities.VerbaleDocumento> rows, CancellationToken ct = default);

    Task UpdateApprestamentiAsync(
        Guid verbaleId, IEnumerable<Entities.VerbaleApprestamento> rows, CancellationToken ct = default);

    Task UpdateCondizioniAsync(
        Guid verbaleId, IEnumerable<Entities.VerbaleCondizioneAmbientale> rows, CancellationToken ct = default);

    // -------- liste Home -------------------------------------------------
    // Verbali firmati / chiusi del giorno richiesto. Le bozze NON sono incluse
    // (hanno una sezione dedicata in Home).
    Task<IReadOnlyList<VerbaleListItem>> GetVerbaliDelGiornoAsync(DateOnly data, CancellationToken ct = default);

    // Tutte le bozze attive ordinate per ultima modifica.
    Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default);
}
