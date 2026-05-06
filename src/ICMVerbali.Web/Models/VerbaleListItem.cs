using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Models;

// DTO read-only per le liste della Home. Contiene i campi joinati (ubicazione
// cantiere, ragioni sociali) per evitare N+1 lato UI. Numero/Anno sono nullable
// perche' assegnati solo al passaggio Bozza -> FirmatoCse (§9.10).
public sealed record VerbaleListItem(
    Guid Id,
    int? Numero,
    int? Anno,
    DateOnly Data,
    string CantiereUbicazione,
    string CommittenteRagioneSociale,
    string ImpresaAppaltatriceRagioneSociale,
    StatoVerbale Stato,
    DateTime UpdatedAt);
