namespace ICMVerbali.Web.Entities;

// Anagrafica persone usata sia per le 4 figure di legge (RL/CSP/CSE/DL) sia per
// le presenze al sopralluogo. "Azienda" e' testo libero (non FK) perche' nel PDF
// compaiono aziende che non sono ne' Committente ne' ImpresaAppaltatrice del
// verbale (es. "ICM Solutions", "Crosslog Srl").
public sealed class Persona
{
    public Guid Id { get; set; }
    public string Nominativo { get; set; } = string.Empty;
    public string Azienda { get; set; } = string.Empty;
    public bool IsAttivo { get; set; }
}
