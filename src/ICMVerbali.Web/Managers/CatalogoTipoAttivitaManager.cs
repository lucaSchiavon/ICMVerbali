using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class CatalogoTipoAttivitaManager : ICatalogoTipoAttivitaManager
{
    private readonly ICatalogoTipoAttivitaRepository _repo;

    public CatalogoTipoAttivitaManager(ICatalogoTipoAttivitaRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CatalogoTipoAttivita>> ListaAttiveAsync(CancellationToken ct = default)
        => _repo.GetAllAttiveAsync(ct);

    public Task<CatalogoTipoAttivita?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);
}
