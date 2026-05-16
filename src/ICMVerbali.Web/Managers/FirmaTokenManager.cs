using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;
using Microsoft.Extensions.Options;

namespace ICMVerbali.Web.Managers;

public sealed class FirmaTokenManager : IFirmaTokenManager
{
    private readonly IFirmaTokenRepository _repo;
    private readonly IVerbaleRepository _verbaleRepo;
    private readonly TimeProvider _clock;
    private readonly FirmaTokenOptions _options;

    public FirmaTokenManager(
        IFirmaTokenRepository repo,
        IVerbaleRepository verbaleRepo,
        TimeProvider clock,
        IOptions<FirmaTokenOptions> options)
    {
        _repo = repo;
        _verbaleRepo = verbaleRepo;
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

        // Ordine dei check (B.12): la revoca esplicita prevale sui motivi temporali.
        // Se il CSE ha rigenerato il link, l'impresa deve sapere che il link e' stato
        // sostituito (non che e' "scaduto"), per chiedere quello nuovo.
        if (entity.RevocatoUtc is not null)
            throw new FirmaTokenInvalidoException(
                FirmaTokenInvalidoMotivo.Revocato,
                $"Token {token} revocato il {entity.RevocatoUtc:O}.");

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

    public Task<FirmaToken?> GetLinkAttivoAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetUltimoAttivoAsync(verbaleId, ct);

    public async Task<Guid> RigeneraTokenAsync(
        Guid verbaleId,
        Guid utenteId,
        CancellationToken ct = default)
    {
        // Pre-check: solo i verbali in FirmatoCse hanno senso da rigenerare.
        // Su Bozza il token non esiste ancora (lo crea FirmaCseAsync), su
        // FirmatoImpresa la firma e' gia' stata applicata e il token e' usato.
        var verbale = await _verbaleRepo.GetByIdAsync(verbaleId, ct)
            ?? throw new InvalidOperationException($"Verbale {verbaleId} non trovato.");

        if (verbale.Stato != StatoVerbale.FirmatoCse)
            throw new InvalidOperationException(
                $"Verbale {verbaleId} non e' in FirmatoCse (stato attuale: {verbale.Stato}). " +
                "La rigenerazione del magic-link e' consentita solo dopo la firma CSE e " +
                "prima della firma Impresa.");

        var seed = CalcolaProssimoToken();
        var inputs = new FirmaTokenInputs(
            seed.TokenId, seed.Token, seed.ScadenzaUtc, seed.CreatedAt);

        await _repo.RigeneraAsync(verbaleId, inputs, utenteId, ct);
        return seed.Token;
    }
}
