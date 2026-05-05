using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class CantiereManager : ICantiereManager
{
    private readonly ICantiereRepository _repo;

    public CantiereManager(ICantiereRepository repo) => _repo = repo;

    public async Task<Cantiere> CreaAsync(string ubicazione, string tipologia, decimal? importoAppalto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ubicazione))
            throw new ArgumentException("Ubicazione obbligatoria.", nameof(ubicazione));
        if (string.IsNullOrWhiteSpace(tipologia))
            throw new ArgumentException("Tipologia obbligatoria.", nameof(tipologia));

        var cantiere = new Cantiere
        {
            Id = Guid.NewGuid(),
            Ubicazione = ubicazione,
            Tipologia = tipologia,
            ImportoAppalto = importoAppalto,
            IsAttivo = true,
        };
        await _repo.CreateAsync(cantiere, ct);
        return cantiere;
    }

    public async Task AggiornaAsync(Cantiere cantiere, CancellationToken ct = default)
    {
        if (cantiere.Id == Guid.Empty)
            throw new ArgumentException("Id cantiere mancante.", nameof(cantiere));
        if (string.IsNullOrWhiteSpace(cantiere.Ubicazione))
            throw new ArgumentException("Ubicazione obbligatoria.");
        if (string.IsNullOrWhiteSpace(cantiere.Tipologia))
            throw new ArgumentException("Tipologia obbligatoria.");

        await _repo.UpdateAsync(cantiere, ct);
    }

    public Task<Cantiere?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Cantiere>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAttiviAsync(ct);

    public Task<IReadOnlyList<Cantiere>> ListaTuttiAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);
}
