using System.Security.Claims;
using ICMVerbali.Web.Authentication;
using ICMVerbali.Web.Components;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// Authentication (cookie) + authorization. Vedi docs/01-design.md §4.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".ICMVerbali.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // dev HTTP / prod HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(RuoloUtente.Admin.ToString()));
});

builder.Services.AddCascadingAuthenticationState();

// Hashing password + seed admin iniziale.
builder.Services.Configure<AdminSeederOptions>(
    builder.Configuration.GetSection(AdminSeederOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddHostedService<DatabaseSeeder>();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Login / logout endpoints (Minimal API). Antiforgery applicato dal middleware.
app.MapPost("/auth/login", async (
    HttpContext ctx,
    IUtenteManager utenteManager,
    IPasswordHasherService hasher) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect(BuildLoginRedirect(returnUrl, "Username e password obbligatori."));
    }

    var utente = await utenteManager.GetByUsernameAsync(username);
    if (utente is null || !utente.IsAttivo
        || !hasher.VerifyHashedPassword(utente.PasswordHash, password))
    {
        return Results.Redirect(BuildLoginRedirect(returnUrl, "Credenziali non valide."));
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, utente.Id.ToString()),
        new(ClaimTypes.Name, utente.Username),
        new(ClaimTypes.Role, utente.Ruolo.ToString()),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();

static string BuildLoginRedirect(string? returnUrl, string error)
{
    var query = $"?ErrorMessage={Uri.EscapeDataString(error)}";
    if (!string.IsNullOrEmpty(returnUrl))
        query += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
    return "/login" + query;
}
