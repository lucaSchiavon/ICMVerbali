using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace ICMVerbali.Web.Authentication;

// IHostedService idempotente che crea l'utente admin iniziale se non esiste.
// Va invocato all'avvio dell'app. Se la password non e' configurata, logga
// warning e salta (non e' un errore di startup).
//
// Vedi docs/01-design.md §9.4.
public sealed class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IOptions<AdminSeederOptions> _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IServiceProvider services,
        IOptions<AdminSeederOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await SeedAdminAsync(ct);
        }
        catch (Exception ex)
        {
            // Niente crash dell'app per errore di seed: logga e prosegui.
            _logger.LogError(ex, "DatabaseSeeder: errore durante il seed admin. L'app continua comunque.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var opts = _options.Value;

        if (string.IsNullOrEmpty(opts.DefaultPassword))
        {
            _logger.LogWarning(
                "DatabaseSeeder: 'Admin:DefaultPassword' non configurata, skip creazione admin. " +
                "Per impostarla: dotnet user-secrets set \"Admin:DefaultPassword\" \"<password>\" --project src/ICMVerbali.Web");
            return;
        }

        if (string.IsNullOrWhiteSpace(opts.DefaultUsername))
        {
            _logger.LogWarning("DatabaseSeeder: 'Admin:DefaultUsername' non configurata, skip creazione admin.");
            return;
        }

        using var scope = _services.CreateScope();
        var utenteRepo = scope.ServiceProvider.GetRequiredService<IUtenteRepository>();
        var utenteManager = scope.ServiceProvider.GetRequiredService<IUtenteManager>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        var existing = await utenteRepo.GetByUsernameAsync(opts.DefaultUsername, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "DatabaseSeeder: utente '{Username}' esiste gia', skip.", opts.DefaultUsername);
            return;
        }

        var hash = hasher.HashPassword(opts.DefaultPassword);
        await utenteManager.CreaAsync(opts.DefaultUsername, null, hash, RuoloUtente.Admin, ct);
        _logger.LogInformation(
            "DatabaseSeeder: creato utente admin '{Username}'.", opts.DefaultUsername);
    }
}
