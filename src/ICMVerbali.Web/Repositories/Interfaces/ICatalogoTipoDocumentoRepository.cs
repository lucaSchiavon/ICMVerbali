using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface ICatalogoTipoDocumentoRepository
{
    Task<CatalogoTipoDocumento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogoTipoDocumento>> GetAllAttiviAsync(CancellationToken ct = default);
}
