namespace ICMVerbali.Web.Entities;

// Token a uso singolo per la firma dell'Impresa Appaltatrice (B.11).
// Generato nella stessa transazione di VerbaleRepository.FirmaCseAsync per
// garantire l'invariante: non esiste un verbale FirmatoCse senza il suo token.
// Vedi docs/01-design.md Addendum 2026-05-14.
public sealed class FirmaToken
{
    public Guid Id { get; set; }
    public Guid VerbaleId { get; set; }

    // GUID v7 distinto da Id: esposto nell'URL della pagina di firma, non rivela
    // la PK interna e si presta a una eventuale rotazione futura.
    public Guid Token { get; set; }

    public DateTime ScadenzaUtc { get; set; }

    // Uso singolo: NULL finche' il token non e' stato consumato dalla firma impresa.
    public DateTime? UsatoUtc { get; set; }

    public DateTime CreatedAt { get; set; }

    // Revoca esplicita: valorizzata quando il CSE rigenera il magic-link (B.12).
    // I token revocati restano in tabella per audit; ValidaTokenAsync li rifiuta
    // con motivo Revocato e RigeneraAsync invalida tutti gli attivi del verbale
    // prima di inserirne uno nuovo.
    public DateTime? RevocatoUtc { get; set; }
}
