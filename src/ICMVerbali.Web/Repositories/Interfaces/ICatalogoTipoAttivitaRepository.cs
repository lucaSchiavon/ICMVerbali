using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface ICatalogoTipoAttivitaRepository
{
    Task<CatalogoTipoAttivita?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogoTipoAttivita>> GetAllAttiveAsync(CancellationToken ct = default);
}
