using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Entities;

// Aggregate root del dominio. Le entita' figlie (Presenza, VerbaleAttivita,
// VerbaleDocumento, VerbaleApprestamento, VerbaleCondizioneAmbientale,
// PrescrizioneCse, Foto, Firma, VerbaleAudit) hanno una sola FK verso Verbale
// con cascade delete. Vedi docs/01-design.md §2.4.
public sealed class Verbale
{
    public Guid Id { get; set; }

    // Numero e Anno assegnati al passaggio Bozza -> FirmatoCse per non bruciare
    // numeri su bozze cancellate (vedi §9.10). Restano null finche' siamo in Bozza.
    public int? Numero { get; set; }
    public int? Anno { get; set; }

    public DateOnly Data { get; set; }

    public Guid CantiereId { get; set; }
    public Guid CommittenteId { get; set; }
    public Guid ImpresaAppaltatriceId { get; set; }

    // Le 4 figure di legge: Responsabile Lavori, Coordinatore Sicurezza
    // Progettazione, Coordinatore Sicurezza Esecuzione, Direttore Lavori.
    // FK verso la stessa tabella Persona, una persona puo' coprire piu' ruoli.
    public Guid RlPersonaId { get; set; }
    public Guid CspPersonaId { get; set; }
    public Guid CsePersonaId { get; set; }
    public Guid DlPersonaId { get; set; }

    // Campi compilati durante la stesura: nullable per ammettere Bozze incomplete.
    // Validazione "hard" applicata al passaggio Bozza -> FirmatoCse (§9.22).
    public EsitoVerifica? Esito { get; set; }
    public CondizioneMeteo? Meteo { get; set; }
    public int? TemperaturaCelsius { get; set; }
    public GestioneInterferenze? Interferenze { get; set; }
    public string? InterferenzeNote { get; set; }

    public StatoVerbale Stato { get; set; }

    public Guid CompilatoDaUtenteId { get; set; }

    // Soft delete logico: i verbali firmati hanno valenza legale (D.Lgs. 81/2008),
    // niente hard delete. Vedi §9.6.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
