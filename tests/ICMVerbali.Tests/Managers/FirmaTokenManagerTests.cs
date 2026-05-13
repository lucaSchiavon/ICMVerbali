using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;
using Microsoft.Extensions.Options;

namespace ICMVerbali.Tests.Managers;

// Test unitari del FirmaTokenManager: niente DB, repo finto + FakeTimeProvider
// per controllare l'orologio. Verifica scadenza, uso singolo, calcolo seed.
public class FirmaTokenManagerTests
{
    [Fact]
    public void CalcolaProssimoToken_usa_ore_da_options_e_clock_corrente()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc));
        var options = Options.Create(new FirmaTokenOptions { ScadenzaOreDefault = 24 });
        var manager = new FirmaTokenManager(new FakeRepo(), clock, options);

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
            repo, clock, Options.Create(new FirmaTokenOptions()));
        var ex = await Assert.ThrowsAsync<FirmaTokenInvalidoException>(() =>
            manager.ValidaTokenAsync(token));
        Assert.Equal(FirmaTokenInvalidoMotivo.Scaduto, ex.Motivo);
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

    private static FirmaTokenManager BuildManager(IFirmaTokenRepository repo)
        => new(
            repo,
            TimeProvider.System,
            Options.Create(new FirmaTokenOptions()));

    private sealed class FakeRepo : IFirmaTokenRepository
    {
        public Dictionary<Guid, FirmaToken> Tokens { get; } = new();

        public Task<FirmaToken?> GetByTokenAsync(Guid token, CancellationToken ct = default)
            => Task.FromResult(Tokens.TryGetValue(token, out var t) ? t : null);

        public Task<IReadOnlyList<FirmaToken>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FirmaToken>>(
                Tokens.Values.Where(t => t.VerbaleId == verbaleId).ToList());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FakeTimeProvider(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
