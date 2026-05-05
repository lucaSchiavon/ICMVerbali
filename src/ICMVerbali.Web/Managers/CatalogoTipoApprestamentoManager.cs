using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class CatalogoTipoApprestamentoManager : ICatalogoTipoApprestamentoManager
{
    private readonly ICatalogoTipoApprestamentoRepository _repo;

    public CatalogoTipoApprestamentoManager(ICatalogoTipoApprestamentoRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CatalogoTipoApprestamento>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAllAttiviAsync(ct);

    public Task<CatalogoTipoApprestamento?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);
}
