using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface ICatalogoTipoAttivitaManager
{
    Task<IReadOnlyList<CatalogoTipoAttivita>> ListaAttiveAsync(CancellationToken ct = default);
    Task<CatalogoTipoAttivita?> GetAsync(Guid id, CancellationToken ct = default);
}
