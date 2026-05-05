using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class ImpresaAppaltatriceManager : IImpresaAppaltatriceManager
{
    private readonly IImpresaAppaltatriceRepository _repo;

    public ImpresaAppaltatriceManager(IImpresaAppaltatriceRepository repo) => _repo = repo;

    public async Task<ImpresaAppaltatrice> CreaAsync(
        string ragioneSociale,
        string? indirizzo,
        string? codiceFiscale,
        string? partitaIva,
        string? numeroIscrizioneRegistroImprese,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ragioneSociale))
            throw new ArgumentException("Ragione sociale obbligatoria.", nameof(ragioneSociale));

        var impresa = new ImpresaAppaltatrice
        {
            Id = Guid.NewGuid(),
            RagioneSociale = ragioneSociale,
            Indirizzo = indirizzo,
            CodiceFiscale = codiceFiscale,
            PartitaIva = partitaIva,
            NumeroIscrizioneRegistroImprese = numeroIscrizioneRegistroImprese,
            IsAttivo = true,
        };
        await _repo.CreateAsync(impresa, ct);
        return impresa;
    }

    public async Task AggiornaAsync(ImpresaAppaltatrice impresa, CancellationToken ct = default)
    {
        if (impresa.Id == Guid.Empty)
            throw new ArgumentException("Id impresa mancante.", nameof(impresa));
        if (string.IsNullOrWhiteSpace(impresa.RagioneSociale))
            throw new ArgumentException("Ragione sociale obbligatoria.");

        await _repo.UpdateAsync(impresa, ct);
    }

    public Task<ImpresaAppaltatrice?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<ImpresaAppaltatrice>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAttiviAsync(ct);

    public Task<IReadOnlyList<ImpresaAppaltatrice>> ListaTuttiAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);
}
