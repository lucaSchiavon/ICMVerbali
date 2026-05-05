namespace ICMVerbali.Web.Entities.Enums;

// Mappa le sottosezioni 5.1 / 5.2 / 5.3 / 5.4 della sez. 5 del PDF.
// Valori 1-4 corrispondono al numero di sottosezione mostrato sul modulo cartaceo.
public enum SottosezioneApprestamento : byte
{
    Organizzazione = 1,
    CaduteDallAlto = 2,
    EmergenzeEDpi = 3,
    Impianti = 4,
}
