namespace ICMVerbali.Web.Components.Shared;

// Stato di auto-save mostrato dalla AutoSaveBadge accanto al titolo dello step.
// Pattern blur-based: l'utente esce dal campo -> salva -> mostra "Salvato".
public enum AutoSaveStatus
{
    Idle,
    Saving,
    Saved,
    Error,
}
