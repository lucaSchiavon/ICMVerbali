using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface IImpresaAppaltatriceManager
{
    Task<ImpresaAppaltatrice> CreaAsync(
        string ragioneSociale,
        string? indirizzo,
        string? codiceFiscale,
        string? partitaIva,
        string? numeroIscrizioneRegistroImprese,
        CancellationToken ct = default);

    Task AggiornaAsync(ImpresaAppaltatrice impresa, CancellationToken ct = default);
    Task<ImpresaAppaltatrice?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ImpresaAppaltatrice>> ListaAttiviAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ImpresaAppaltatrice>> ListaTuttiAsync(CancellationToken ct = default);
}
