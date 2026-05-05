using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface ICommittenteManager
{
    Task<Committente> CreaAsync(
        string ragioneSociale,
        string? indirizzo,
        string? codiceFiscale,
        string? partitaIva,
        string? numeroIscrizioneRegistroImprese,
        CancellationToken ct = default);

    Task<Committente?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Committente>> ListaAttiviAsync(CancellationToken ct = default);
}
