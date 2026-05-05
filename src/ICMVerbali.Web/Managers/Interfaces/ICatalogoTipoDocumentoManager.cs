using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface ICatalogoTipoDocumentoManager
{
    Task<IReadOnlyList<CatalogoTipoDocumento>> ListaAttiviAsync(CancellationToken ct = default);
    Task<CatalogoTipoDocumento?> GetAsync(Guid id, CancellationToken ct = default);
}
