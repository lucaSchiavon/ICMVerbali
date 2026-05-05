namespace ICMVerbali.Web.Entities.Enums;

// Tipi di evento tracciati nella tabella VerbaleAudit (vedi docs/01-design.md §9.12).
// Lista minimale; nuovi tipi vanno appesi mantenendo i valori numerici esistenti.
public enum EventoAuditTipo : byte
{
    Creazione = 0,
    TransizioneStato = 1,
    Eliminazione = 2,
}
