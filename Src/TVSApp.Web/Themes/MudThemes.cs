using MudBlazor;

namespace TVSApp.Web.Themes;

public static class MudThemes
{
    public static MudTheme MyThemes = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#BE0405",
            Secondary = "#FFFFFF",
            Tertiary = "#000000"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Open Sans", "sans-serif"]
            }
        }
    };
}