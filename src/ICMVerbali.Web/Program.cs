using ICMVerbali.Web.Components;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Managers;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;
using Microsoft.AspNetCore.Components.Server;
using MudBlazor.Services;

// Type handler Dapper (DateOnly etc.). Idempotente.
DapperConfiguration.Initialize();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Circuit options tarate per uso in cantiere su connettivita' instabile.
// Vedi docs/01-design.md §10.1.
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DisconnectedCircuitMaxRetained = 200;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
    options.MaxBufferedUnacknowledgedRenderBatches = 20;
});

// Data access (Dapper) e storage immagini. Vedi docs/01-design.md §3, §7.
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IFotoStorageService, LocalFotoStorageService>();

// Repository (Dapper). Lifetime Scoped: una connessione per richiesta HTTP.
builder.Services.AddScoped<ICantiereRepository, CantiereRepository>();
builder.Services.AddScoped<ICommittenteRepository, CommittenteRepository>();
builder.Services.AddScoped<IImpresaAppaltatriceRepository, ImpresaAppaltatriceRepository>();
builder.Services.AddScoped<IPersonaRepository, PersonaRepository>();
builder.Services.AddScoped<IUtenteRepository, UtenteRepository>();
builder.Services.AddScoped<ICatalogoTipoAttivitaRepository, CatalogoTipoAttivitaRepository>();
builder.Services.AddScoped<ICatalogoTipoDocumentoRepository, CatalogoTipoDocumentoRepository>();
builder.Services.AddScoped<ICatalogoTipoApprestamentoRepository, CatalogoTipoApprestamentoRepository>();
builder.Services.AddScoped<ICatalogoTipoCondizioneAmbientaleRepository, CatalogoTipoCondizioneAmbientaleRepository>();
builder.Services.AddScoped<IVerbaleRepository, VerbaleRepository>();

// Manager. Lifetime Scoped (1:1 con Repository).
builder.Services.AddScoped<ICantiereManager, CantiereManager>();
builder.Services.AddScoped<ICommittenteManager, CommittenteManager>();
builder.Services.AddScoped<IImpresaAppaltatriceManager, ImpresaAppaltatriceManager>();
builder.Services.AddScoped<IPersonaManager, PersonaManager>();
builder.Services.AddScoped<IUtenteManager, UtenteManager>();
builder.Services.AddScoped<ICatalogoTipoAttivitaManager, CatalogoTipoAttivitaManager>();
builder.Services.AddScoped<ICatalogoTipoDocumentoManager, CatalogoTipoDocumentoManager>();
builder.Services.AddScoped<ICatalogoTipoApprestamentoManager, CatalogoTipoApprestamentoManager>();
builder.Services.AddScoped<ICatalogoTipoCondizioneAmbientaleManager, CatalogoTipoCondizioneAmbientaleManager>();
builder.Services.AddScoped<IVerbaleManager, VerbaleManager>();

var app = builder.Build();

// Boot-time validation: forziamo la risoluzione dei servizi infrastrutturali per
// far emergere subito errori di configurazione (connection string mancante,
// UploadsBasePath non valido, ecc.) invece di scoprirli alla prima richiesta.
_ = app.Services.GetRequiredService<ISqlConnectionFactory>();
_ = app.Services.GetRequiredService<IFotoStorageService>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
