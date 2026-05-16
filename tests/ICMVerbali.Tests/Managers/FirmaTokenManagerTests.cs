using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers;
using ICMVerbali.Web.Models;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;
using Microsoft.Extensions.Options;

namespace ICMVerbali.Tests.Managers;

// Test unitari del FirmaTokenManager: niente DB, repo finto + FakeTimeProvider
// per controllare l'orologio. Verifica scadenza, uso singolo, revoca, calcolo
// seed e rigenerazione (B.12).
public class FirmaTokenManagerTests
{
    [Fact]
    public void CalcolaProssimoToken_usa_ore_da_options_e_clock_corrente()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc));
        var options = Options.Create(new FirmaTokenOptions { ScadenzaOreDefault = 24 });
        var manager = new FirmaTokenManager(new FakeRepo(), new FakeVerbaleRepo(), clock, options);

        var seed = manager.CalcolaProssimoToken();

        Assert.NotEqual(Guid.Empty, seed.TokenId);
        Assert.NotEqual(Guid.Empty, seed.Token);
        Assert.NotEqual(seed.TokenId, seed.Token);
        Assert.Equal(new DateTime(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc), seed.CreatedAt);
        Assert.Equal(new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc), seed.ScadenzaUtc);
    }

    [Fact]
    public async Task ValidaTokenAsync_token_inesistente_lancia_NonTrovato()
    {
        var manager = BuildManager(new FakeRepo());
        var ex = await Assert.ThrowsAsync<FirmaTokenInvalidoException>(() =>
            manager.ValidaTokenAsync(Guid.CreateVersion7()));
        Assert.Equal(FirmaTokenInvalidoMotivo.NonTrovato, ex.Motivo);
    }

    [Fact]
    public async Task ValidaTokenAsync_token_gia_usato_lancia_GiaUsato()
    {
        var token = Guid.CreateVersion7();
        var repo = new FakeRepo();
        repo.Tokens[token] = new FirmaToken
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = Guid.CreateVersion7(),
            Token = token,
            ScadenzaUtc = DateTime.UtcNow.AddHours(10),
            UsatoUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
        };

        var manager = BuildManager(repo);
        var ex = await Assert.ThrowsAsync<FirmaTokenInvalidoException>(() =>
            manager.ValidaTokenAsync(token));
        Assert.Equal(FirmaTokenInvalidoMotivo.GiaUsato, ex.Motivo);
    }

    [Fact]
    public async Task ValidaTokenAsync_token_scaduto_lancia_Scaduto()
    {
        var token = Guid.CreateVersion7();
        var now = new DateTime(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FakeTimeProvider(now);
        var repo = new FakeRepo();
        repo.Tokens[token] = new FirmaToken
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = Guid.CreateVersion7(),
            Token = token,
            // Scadenza nel passato rispetto a clock.
            ScadenzaUtc = now.AddMinutes(-1),
            UsatoUtc = null,
            CreatedAt = now.AddHours(-48),
        };

        var manager = new FirmaTokenManager(
            repo, new FakeVerbaleRepo(), clock, Options.Create(new FirmaTokenOptions()));
        var ex = await Assert.ThrowsAsync<FirmaTokenInvalidoException>(() =>
            manager.ValidaTokenAsync(token));
        Assert.Equal(FirmaTokenInvalidoMotivo.Scaduto, ex.Motivo);
    }

    [Fact]
    public async Task ValidaTokenAsync_token_revocato_lancia_Revocato()
    {
        // B.12: la revoca esplicita prevale su scadenza/usato per non confondere
        // l'utente con un messaggio "scaduto" quando in realta' il CSE ha sostituito
        // il link.
        var token = Guid.CreateVersion7();
        var repo = new FakeRepo();
        repo.Tokens[token] = new FirmaToken
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = Guid.CreateVersion7(),
            Token = token,
            ScadenzaUtc = DateTime.UtcNow.AddHours(10),
            UsatoUtc = null,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            RevocatoUtc = DateTime.UtcNow.AddMinutes(-5),
        };

        var manager = BuildManager(repo);
        var ex = await Assert.ThrowsAsync<FirmaTokenInvalidoException>(() =>
            manager.ValidaTokenAsync(token));
        Assert.Equal(FirmaTokenInvalidoMotivo.Revocato, ex.Motivo);
    }

    [Fact]
    public async Task ValidaTokenAsync_token_valido_ritorna_entity()
    {
        var token = Guid.CreateVersion7();
        var entity = new FirmaToken
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = Guid.CreateVersion7(),
            Token = token,
            ScadenzaUtc = DateTime.UtcNow.AddHours(10),
            UsatoUtc = null,
            CreatedAt = DateTime.UtcNow,
        };
        var repo = new FakeRepo();
        repo.Tokens[token] = entity;

        var manager = BuildManager(repo);
        var result = await manager.ValidaTokenAsync(token);

        Assert.Same(entity, result);
    }

    [Fact]
    public async Task RigeneraTokenAsync_su_FirmatoCse_genera_nuovo_token_e_chiama_repo()
    {
        var verbaleId = Guid.CreateVersion7();
        var utenteId = Guid.CreateVersion7();
        var verbaleRepo = new FakeVerbaleRepo();
        verbaleRepo.SetVerbale(new Verbale
        {
            Id = verbaleId,
            Stato = StatoVerbale.FirmatoCse,
            CompilatoDaUtenteId = utenteId,
        });
        var repo = new FakeRepo();

        var manager = BuildManager(repo, verbaleRepo);
        var nuovoToken = await manager.RigeneraTokenAsync(verbaleId, utenteId);

        Assert.NotEqual(Guid.Empty, nuovoToken);
        var chiamate = repo.RigeneraChiamate;
        var (vid, inputs, uid) = Assert.Single(chiamate);
        Assert.Equal(verbaleId, vid);
        Assert.Equal(utenteId, uid);
        Assert.Equal(nuovoToken, inputs.Token);
        Assert.NotEqual(Guid.Empty, inputs.TokenId);
        Assert.NotEqual(inputs.TokenId, inputs.Token);
    }

    [Theory]
    [InlineData(StatoVerbale.Bozza)]
    [InlineData(StatoVerbale.FirmatoImpresa)]
    [InlineData(StatoVerbale.Chiuso)]
    public async Task RigeneraTokenAsync_su_stato_diverso_da_FirmatoCse_throw(StatoVerbale stato)
    {
        var verbaleId = Guid.CreateVersion7();
        var verbaleRepo = new FakeVerbaleRepo();
        verbaleRepo.SetVerbale(new Verbale
        {
            Id = verbaleId,
            Stato = stato,
            CompilatoDaUtenteId = Guid.CreateVersion7(),
        });
        var repo = new FakeRepo();

        var manager = BuildManager(repo, verbaleRepo);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.RigeneraTokenAsync(verbaleId, Guid.CreateVersion7()));
        Assert.Empty(repo.RigeneraChiamate);
    }

    [Fact]
    public async Task RigeneraTokenAsync_verbale_inesistente_throw()
    {
        var manager = BuildManager(new FakeRepo(), new FakeVerbaleRepo());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.RigeneraTokenAsync(Guid.CreateVersion7(), Guid.CreateVersion7()));
    }

    [Fact]
    public async Task GetLinkAttivoAsync_passthrough_al_repository()
    {
        var verbaleId = Guid.CreateVersion7();
        var atteso = new FirmaToken
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            Token = Guid.CreateVersion7(),
            ScadenzaUtc = DateTime.UtcNow.AddHours(10),
            CreatedAt = DateTime.UtcNow,
        };
        var repo = new FakeRepo();
        repo.UltimoAttivoPerVerbale[verbaleId] = atteso;

        var manager = BuildManager(repo);
        var risultato = await manager.GetLinkAttivoAsync(verbaleId);

        Assert.Same(atteso, risultato);
    }

    private static FirmaTokenManager BuildManager(IFirmaTokenRepository repo)
        => BuildManager(repo, new FakeVerbaleRepo());

    private static FirmaTokenManager BuildManager(
        IFirmaTokenRepository repo, IVerbaleRepository verbaleRepo)
        => new(
            repo,
            verbaleRepo,
            TimeProvider.System,
            Options.Create(new FirmaTokenOptions()));

    private sealed class FakeRepo : IFirmaTokenRepository
    {
        public Dictionary<Guid, FirmaToken> Tokens { get; } = new();
        public Dictionary<Guid, FirmaToken?> UltimoAttivoPerVerbale { get; } = new();
        public List<(Guid VerbaleId, FirmaTokenInputs Inputs, Guid UtenteId)> RigeneraChiamate { get; } = new();

        public Task<FirmaToken?> GetByTokenAsync(Guid token, CancellationToken ct = default)
            => Task.FromResult(Tokens.TryGetValue(token, out var t) ? t : null);

        public Task<IReadOnlyList<FirmaToken>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FirmaToken>>(
                Tokens.Values.Where(t => t.VerbaleId == verbaleId).ToList());

        public Task<FirmaToken?> GetUltimoAttivoAsync(Guid verbaleId, CancellationToken ct = default)
            => Task.FromResult(UltimoAttivoPerVerbale.TryGetValue(verbaleId, out var t) ? t : null);

        public Task RigeneraAsync(Guid verbaleId, FirmaTokenInputs nuovoToken, Guid utenteId, CancellationToken ct = default)
        {
            RigeneraChiamate.Add((verbaleId, nuovoToken, utenteId));
            return Task.CompletedTask;
        }
    }

    // Stub minimale: implementa solo GetByIdAsync (l'unico chiamato dal manager).
    // Tutti gli altri metodi lanciano NotImplementedException — se un test li
    // invoca per sbaglio, fallisce in modo esplicito.
    private sealed class FakeVerbaleRepo : IVerbaleRepository
    {
        private readonly Dictionary<Guid, Verbale> _store = new();

        public void SetVerbale(Verbale v) => _store[v.Id] = v;

        public Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var v) ? v : null);

        // -------- non utilizzati nei test del FirmaTokenManager --------------
        public Task CreateBozzaWithChildrenAsync(Verbale verbale, IEnumerable<VerbaleAttivita> attivita, IEnumerable<VerbaleDocumento> documenti, IEnumerable<VerbaleApprestamento> apprestamenti, IEnumerable<VerbaleCondizioneAmbientale> condizioniAmbientali, VerbaleAudit audit, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateAnagraficaAsync(Guid id, DateOnly data, Guid cantiereId, Guid committenteId, Guid impresaAppaltatriceId, Guid rlPersonaId, Guid cspPersonaId, Guid csePersonaId, Guid dlPersonaId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateMeteoEsitoAsync(Guid id, EsitoVerifica? esito, CondizioneMeteo? meteo, int? temperaturaCelsius, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateInterferenzeAsync(Guid id, GestioneInterferenze? interferenze, string? interferenzeNote, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<VerbaleAttivitaItem>> GetAttivitaByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<VerbaleDocumentoItem>> GetDocumentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<VerbaleApprestamentoItem>> GetApprestamentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<VerbaleCondizioneAmbientaleItem>> GetCondizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateAttivitaBulkAsync(Guid verbaleId, IEnumerable<VerbaleAttivita> rows, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateDocumentiBulkAsync(Guid verbaleId, IEnumerable<VerbaleDocumento> rows, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateApprestamentiBulkAsync(Guid verbaleId, IEnumerable<VerbaleApprestamento> rows, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateCondizioniBulkAsync(Guid verbaleId, IEnumerable<VerbaleCondizioneAmbientale> rows, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<PrescrizioneCse>> GetPrescrizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task ReplacePrescrizioniAsync(Guid verbaleId, IEnumerable<PrescrizioneCse> rows, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<VerbaleListItem>> GetByDataAsync(DateOnly data, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<FirmaCseResult> FirmaCseAsync(Guid verbaleId, int anno, string nomeFirmatario, DateOnly dataFirma, string immagineFirmaPath, Guid utenteId, FirmaTokenInputs tokenImpresa, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task FirmaImpresaAsync(Guid verbaleId, Guid tokenId, string nomeFirmatario, DateOnly dataFirma, string immagineFirmaPath, Guid utenteId, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FakeTimeProvider(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
