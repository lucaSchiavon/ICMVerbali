using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface ICatalogoTipoApprestamentoManager
{
    Task<IReadOnlyList<CatalogoTipoApprestamento>> ListaAttiviAsync(CancellationToken ct = default);
    Task<CatalogoTipoApprestamento?> GetAsync(Guid id, CancellationToken ct = default);
}
