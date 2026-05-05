using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class CommittenteManager : ICommittenteManager
{
    private readonly ICommittenteRepository _repo;

    public CommittenteManager(ICommittenteRepository repo) => _repo = repo;

    public async Task<Committente> CreaAsync(
        string ragioneSociale,
        string? indirizzo,
        string? codiceFiscale,
        string? partitaIva,
        string? numeroIscrizioneRegistroImprese,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ragioneSociale))
            throw new ArgumentException("Ragione sociale obbligatoria.", nameof(ragioneSociale));

        var committente = new Committente
        {
            Id = Guid.NewGuid(),
            RagioneSociale = ragioneSociale,
            Indirizzo = indirizzo,
            CodiceFiscale = codiceFiscale,
            PartitaIva = partitaIva,
            NumeroIscrizioneRegistroImprese = numeroIscrizioneRegistroImprese,
            IsAttivo = true,
        };
        await _repo.CreateAsync(committente, ct);
        return committente;
    }

    public async Task AggiornaAsync(Committente committente, CancellationToken ct = default)
    {
        if (committente.Id == Guid.Empty)
            throw new ArgumentException("Id committente mancante.", nameof(committente));
        if (string.IsNullOrWhiteSpace(committente.RagioneSociale))
            throw new ArgumentException("Ragione sociale obbligatoria.");

        await _repo.UpdateAsync(committente, ct);
    }

    public Task<Committente?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Committente>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAttiviAsync(ct);

    public Task<IReadOnlyList<Committente>> ListaTuttiAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);
}
