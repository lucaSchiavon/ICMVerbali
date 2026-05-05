using MudBlazor;

namespace ICMVerbali.Web.Theme;

// Tema base ICMVerbali. Palette navy placeholder: i codici esatti vanno
// raffinati sul logo ICM Solutions ufficiale. Vedi docs/01-design.md §8.5.
// Switch alto contrasto outdoor (§9.18) sara' aggiunto in fase successiva.
public static class AppTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0F2A52",
            Secondary = "#1F4F8F",
            Tertiary = "#5B7FB5",
            AppbarBackground = "#0F2A52",
            AppbarText = Colors.Shades.White,
            Background = "#F5F6F8",
            Surface = Colors.Shades.White,
        }
    };
}
