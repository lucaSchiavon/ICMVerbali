using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers;
using ICMVerbali.Web.Models;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;

namespace ICMVerbali.Tests.Managers;

// Test di unita' per VerbaleManager.UpdatePrescrizioniAsync. Verifica la
// normalizzazione: dedup di Id duplicati (difesa contro la race AutoSave/Submit
// vista live 2026-05-13), scarto righe vuote, rinumerazione Ordine 1..N.
public class VerbaleManagerPrescrizioniTests
{
    [Fact]
    public async Task UpdatePrescrizioniAsync_dedup_di_Id_duplicati_genera_Guid_nuovo()
    {
        var captureRepo = new CaptureVerbaleRepo();
        var manager = BuildManager(captureRepo);
        var verbaleId = Guid.CreateVersion7();

        // Due righe con LO STESSO Id (simula la race AutoSave + Submit con
        // riferimento all'oggetto duplicato). Senza dedup, ReplacePrescrizioniAsync
        // lancerebbe PK violation.
        var idDuplicato = Guid.CreateVersion7();
        var input = new[]
        {
            new PrescrizioneCse { Id = idDuplicato, VerbaleId = verbaleId, Testo = "Prima" },
            new PrescrizioneCse { Id = idDuplicato, VerbaleId = verbaleId, Testo = "Seconda" },
        };

        await manager.UpdatePrescrizioniAsync(verbaleId, input);

        Assert.NotNull(captureRepo.LastWritten);
        var written = captureRepo.LastWritten!;
        Assert.Equal(2, written.Count);
        // Gli Id post-dedup devono essere tutti distinti.
        Assert.Equal(2, written.Select(r => r.Id).Distinct().Count());
        // Almeno una delle due deve mantenere l'Id originale (prima occorrenza).
        Assert.Contains(written, r => r.Id == idDuplicato);
        // Ordine rinumerato 1, 2.
        Assert.Equal(new[] { 1, 2 }, written.Select(r => r.Ordine));
    }

    [Fact]
    public async Task UpdatePrescrizioniAsync_scarta_righe_con_Testo_vuoto_o_whitespace()
    {
        var captureRepo = new CaptureVerbaleRepo();
        var manager = BuildManager(captureRepo);
        var verbaleId = Guid.CreateVersion7();

        var input = new[]
        {
            new PrescrizioneCse { Id = Guid.CreateVersion7(), Testo = "Valida" },
            new PrescrizioneCse { Id = Guid.CreateVersion7(), Testo = "" },
            new PrescrizioneCse { Id = Guid.CreateVersion7(), Testo = "   " },
            new PrescrizioneCse { Id = Guid.CreateVersion7(), Testo = "Altra valida" },
        };

        await manager.UpdatePrescrizioniAsync(verbaleId, input);

        var written = captureRepo.LastWritten!;
        Assert.Equal(2, written.Count);
        Assert.Equal(new[] { "Valida", "Altra valida" }, written.Select(r => r.Testo));
        Assert.Equal(new[] { 1, 2 }, written.Select(r => r.Ordine));
    }

    [Fact]
    public async Task UpdatePrescrizioniAsync_genera_Guid_per_Id_Empty()
    {
        var captureRepo = new CaptureVerbaleRepo();
        var manager = BuildManager(captureRepo);
        var verbaleId = Guid.CreateVersion7();

        var input = new[]
        {
            new PrescrizioneCse { Id = Guid.Empty, Testo = "Nuova" },
        };

        await manager.UpdatePrescrizioniAsync(verbaleId, input);

        var written = captureRepo.LastWritten!;
        Assert.Single(written);
        Assert.NotEqual(Guid.Empty, written[0].Id);
        Assert.Equal(verbaleId, written[0].VerbaleId);
    }

    // ---------- helpers ---------------------------------------------------

    private static VerbaleManager BuildManager(IVerbaleRepository repo)
        => new(
            repo,
            new NoopCatalogoTipoAttivita(),
            new NoopCatalogoTipoDocumento(),
            new NoopCatalogoTipoApprestamento(),
            new NoopCatalogoTipoCondizioneAmbientale(),
            new NoopFirmaStorage(),
            TimeProvider.System);

    private sealed class CaptureVerbaleRepo : IVerbaleRepository
    {
        public IReadOnlyList<PrescrizioneCse>? LastWritten { get; private set; }

        public Task ReplacePrescrizioniAsync(
            Guid verbaleId, IEnumerable<PrescrizioneCse> rows, CancellationToken ct = default)
        {
            LastWritten = rows.ToList();
            return Task.CompletedTask;
        }

        // Membri non usati dai test
        public Task CreateBozzaWithChildrenAsync(Verbale verbale, IEnumerable<VerbaleAttivita> attivita, IEnumerable<VerbaleDocumento> documenti, IEnumerable<VerbaleApprestamento> apprestamenti, IEnumerable<VerbaleCondizioneAmbientale> condizioniAmbientali, VerbaleAudit audit, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Verbale?>(null);
        public Task UpdateAnagraficaAsync(Guid id, DateOnly data, Guid cantiereId, Guid committenteId, Guid impresaAppaltatriceId, Guid rlPersonaId, Guid cspPersonaId, Guid csePersonaId, Guid dlPersonaId, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateMeteoEsitoAsync(Guid id, EsitoVerifica? esito, CondizioneMeteo? meteo, int? temperaturaCelsius, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateInterferenzeAsync(Guid id, GestioneInterferenze? interferenze, string? interferenzeNote, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<VerbaleAttivitaItem>> GetAttivitaByVerbaleAsync(Guid verbaleId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<VerbaleAttivitaItem>>(Array.Empty<VerbaleAttivitaItem>());
        public Task<IReadOnlyList<VerbaleDocumentoItem>> GetDocumentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<VerbaleDocumentoItem>>(Array.Empty<VerbaleDocumentoItem>());
        public Task<IReadOnlyList<VerbaleApprestamentoItem>> GetApprestamentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<VerbaleApprestamentoItem>>(Array.Empty<VerbaleApprestamentoItem>());
        public Task<IReadOnlyList<VerbaleCondizioneAmbientaleItem>> GetCondizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<VerbaleCondizioneAmbientaleItem>>(Array.Empty<VerbaleCondizioneAmbientaleItem>());
        public Task UpdateAttivitaBulkAsync(Guid verbaleId, IEnumerable<VerbaleAttivita> rows, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateDocumentiBulkAsync(Guid verbaleId, IEnumerable<VerbaleDocumento> rows, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateApprestamentiBulkAsync(Guid verbaleId, IEnumerable<VerbaleApprestamento> rows, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateCondizioniBulkAsync(Guid verbaleId, IEnumerable<VerbaleCondizioneAmbientale> rows, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PrescrizioneCse>> GetPrescrizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PrescrizioneCse>>(Array.Empty<PrescrizioneCse>());
        public Task<IReadOnlyList<VerbaleListItem>> GetByDataAsync(DateOnly data, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<VerbaleListItem>>(Array.Empty<VerbaleListItem>());
        public Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<VerbaleListItem>>(Array.Empty<VerbaleListItem>());
        public Task<FirmaCseResult> FirmaCseAsync(Guid verbaleId, int anno, string nomeFirmatario, DateOnly dataFirma, string immagineFirmaPath, Guid utenteId, CancellationToken ct = default) => Task.FromResult(new FirmaCseResult(1, anno));
    }

    private sealed class NoopCatalogoTipoAttivita : ICatalogoTipoAttivitaRepository
    {
        public Task<IReadOnlyList<CatalogoTipoAttivita>> GetAllAttiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CatalogoTipoAttivita>>(Array.Empty<CatalogoTipoAttivita>());
        public Task<CatalogoTipoAttivita?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<CatalogoTipoAttivita?>(null);
    }
    private sealed class NoopCatalogoTipoDocumento : ICatalogoTipoDocumentoRepository
    {
        public Task<IReadOnlyList<CatalogoTipoDocumento>> GetAllAttiviAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CatalogoTipoDocumento>>(Array.Empty<CatalogoTipoDocumento>());
        public Task<CatalogoTipoDocumento?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<CatalogoTipoDocumento?>(null);
    }
    private sealed class NoopCatalogoTipoApprestamento : ICatalogoTipoApprestamentoRepository
    {
        public Task<IReadOnlyList<CatalogoTipoApprestamento>> GetAllAttiviAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CatalogoTipoApprestamento>>(Array.Empty<CatalogoTipoApprestamento>());
        public Task<CatalogoTipoApprestamento?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<CatalogoTipoApprestamento?>(null);
    }
    private sealed class NoopCatalogoTipoCondizioneAmbientale : ICatalogoTipoCondizioneAmbientaleRepository
    {
        public Task<IReadOnlyList<CatalogoTipoCondizioneAmbientale>> GetAllAttiviAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CatalogoTipoCondizioneAmbientale>>(Array.Empty<CatalogoTipoCondizioneAmbientale>());
        public Task<CatalogoTipoCondizioneAmbientale?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<CatalogoTipoCondizioneAmbientale?>(null);
    }
    private sealed class NoopFirmaStorage : IFirmaStorageService
    {
        public Task<FirmaStorageResult> SalvaAsync(Guid verbaleId, TipoFirmatario tipo, byte[] pngBytes, CancellationToken ct = default)
            => Task.FromResult(new FirmaStorageResult("noop", pngBytes.LongLength, "noop"));
        public Task<Stream> ApriLetturaAsync(string filePathRelativo, CancellationToken ct = default)
            => Task.FromResult<Stream>(Stream.Null);
        public Task EliminaAsync(string filePathRelativo, CancellationToken ct = default) => Task.CompletedTask;
    }
}
