using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface ICatalogoTipoCondizioneAmbientaleRepository
{
    Task<CatalogoTipoCondizioneAmbientale?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogoTipoCondizioneAmbientale>> GetAllAttiviAsync(CancellationToken ct = default);
}
