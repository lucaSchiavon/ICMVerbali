using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class CatalogoTipoDocumentoManager : ICatalogoTipoDocumentoManager
{
    private readonly ICatalogoTipoDocumentoRepository _repo;

    public CatalogoTipoDocumentoManager(ICatalogoTipoDocumentoRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CatalogoTipoDocumento>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAllAttiviAsync(ct);

    public Task<CatalogoTipoDocumento?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);
}
