using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;

namespace ICMVerbali.Web.Managers;

public sealed class FotoManager : IFotoManager
{
    private readonly IFotoRepository _repo;
    private readonly IFotoStorageService _storage;

    public FotoManager(IFotoRepository repo, IFotoStorageService storage)
    {
        _repo = repo;
        _storage = storage;
    }

    public Task<IReadOnlyList<Foto>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetByVerbaleAsync(verbaleId, ct);

    public async Task<Foto> UploadAsync(
        Guid verbaleId,
        Stream contenuto,
        string fileName,
        long contentLength,
        CancellationToken ct = default)
    {
        if (contentLength > IFotoManager.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File troppo grande ({contentLength / (1024 * 1024)} MB, max {IFotoManager.MaxFileSizeBytes / (1024 * 1024)} MB).");

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !IFotoManager.AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"Formato non supportato. Accettati: {string.Join(", ", IFotoManager.AllowedExtensions)}.");

        // Lo storage si occupa del resize + thumb; ritorna il path relativo del full.
        var stored = await _storage.SalvaAsync(verbaleId, contenuto, fileName, ct);

        var foto = new Foto
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            FilePathRelativo = stored.FilePathRelativo,
            Didascalia = null,
            // Ordine viene impostato dal repo dentro la transazione (max+1).
            // CreatedAt: lascia DEFAULT del DB.
        };
        await _repo.CreateAsync(foto, ct);

        // Re-fetch per leggere CreatedAt + Ordine assegnati dal DB.
        return await _repo.GetByIdAsync(foto.Id, ct)
            ?? throw new InvalidOperationException("Foto creata ma non rileggibile.");
    }

    public Task UpdateDidascaliaAsync(Guid id, string? didascalia, CancellationToken ct = default)
        => _repo.UpdateDidascaliaAsync(id, didascalia, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // L'ordine: prima togliamo dal DB (transazione del repo), POI dal filesystem.
        // Se il DB delete fallisce, il file resta - pulibile poi con un job. Se
        // riesce ma il file delete fallisce, il file resta orfano ma non causa
        // inconsistenza visibile (la UI legge dal DB).
        var path = await _repo.DeleteAsync(id, ct);
        if (path is null) return;

        try
        {
            await _storage.EliminaAsync(path, ct);
        }
        catch
        {
            // File orfano. Non rialziamo: l'utente vede la foto sparita dalla UI.
            // Eventuale GC dei file orfani in B.10+.
        }
    }

    public Task ReorderAsync(Guid verbaleId, IReadOnlyList<Guid> idsInOrder, CancellationToken ct = default)
    {
        var updates = idsInOrder
            .Select((id, idx) => new FotoOrdineUpdate(id, idx + 1))
            .ToList();
        return _repo.UpdateOrdineBulkAsync(verbaleId, updates, ct);
    }
}
