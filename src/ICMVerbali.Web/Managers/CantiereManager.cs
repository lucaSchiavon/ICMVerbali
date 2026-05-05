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

    public Task<Cantiere?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Cantiere>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAttiviAsync(ct);
}
