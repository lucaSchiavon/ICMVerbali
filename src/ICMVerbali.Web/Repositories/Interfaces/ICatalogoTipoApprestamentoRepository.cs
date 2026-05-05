using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface ICatalogoTipoApprestamentoRepository
{
    Task<CatalogoTipoApprestamento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogoTipoApprestamento>> GetAllAttiviAsync(CancellationToken ct = default);
}
