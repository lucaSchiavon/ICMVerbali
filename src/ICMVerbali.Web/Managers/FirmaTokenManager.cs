using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;
using Microsoft.Extensions.Options;

namespace ICMVerbali.Web.Managers;

public sealed class FirmaTokenManager : IFirmaTokenManager
{
    private readonly IFirmaTokenRepository _repo;
    private readonly TimeProvider _clock;
    private readonly FirmaTokenOptions _options;

    public FirmaTokenManager(
        IFirmaTokenRepository repo,
        TimeProvider clock,
        IOptions<FirmaTokenOptions> options)
    {
        _repo = repo;
        _clock = clock;
        _options = options.Value;
    }

    public FirmaTokenSeed CalcolaProssimoToken()
    {
        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        return new FirmaTokenSeed(
            TokenId: Guid.CreateVersion7(),
            Token: Guid.CreateVersion7(),
            ScadenzaUtc: nowUtc.AddHours(_options.ScadenzaOreDefault),
            CreatedAt: nowUtc);
    }

    public async Task<FirmaToken> ValidaTokenAsync(Guid token, CancellationToken ct = default)
    {
        var entity = await _repo.GetByTokenAsync(token, ct);
        if (entity is null)
            throw new FirmaTokenInvalidoException(
                FirmaTokenInvalidoMotivo.NonTrovato,
                $"Token {token} non trovato.");

        if (entity.UsatoUtc is not null)
            throw new FirmaTokenInvalidoException(
                FirmaTokenInvalidoMotivo.GiaUsato,
                $"Token {token} gia' utilizzato il {entity.UsatoUtc:O}.");

        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        if (entity.ScadenzaUtc <= nowUtc)
            throw new FirmaTokenInvalidoException(
                FirmaTokenInvalidoMotivo.Scaduto,
                $"Token {token} scaduto il {entity.ScadenzaUtc:O}.");

        return entity;
    }
}
