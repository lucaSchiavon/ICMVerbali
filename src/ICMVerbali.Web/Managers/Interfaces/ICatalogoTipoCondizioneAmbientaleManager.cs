using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface ICatalogoTipoCondizioneAmbientaleManager
{
    Task<IReadOnlyList<CatalogoTipoCondizioneAmbientale>> ListaAttiviAsync(CancellationToken ct = default);
    Task<CatalogoTipoCondizioneAmbientale?> GetAsync(Guid id, CancellationToken ct = default);
}
