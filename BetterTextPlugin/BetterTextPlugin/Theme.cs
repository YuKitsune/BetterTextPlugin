using System.Windows;
using System.Windows.Media;

namespace BetterTextPlugin;

public static class Theme
{
    public static float Alpha = 0.4f;
    public static Brush LightBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 255, 255, 255));
    public static Brush DarkBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 0, 0, 0));

    public static SolidColorBrush BackgroundColor { get; set; } = new(ToWindowsColor(System.Drawing.Color.FromArgb(255, 160, 170, 170)));
    public static SolidColorBrush GenericTextColor { get; set; } = new(ToWindowsColor(System.Drawing.Color.FromArgb(255, 96, 0, 0)));
    public static SolidColorBrush InteractiveTextColor { get; set; } = new(ToWindowsColor(System.Drawing.Color.FromArgb(255, 0, 0, 96)));
    public static SolidColorBrush NonInteractiveTextColor { get; set; } = new(ToWindowsColor(System.Drawing.Color.FromArgb(255, 90, 90, 90)));
    public static SolidColorBrush SelectedButtonColor { get; set; } = new(ToWindowsColor(System.Drawing.Color.FromArgb(255, 0, 0, 96)));

    // TODO: Support live updating font sizes
    public static FontFamily FontFamily { get; set; } = new("Terminus (TTF)");
    public static double FontSize { get; set; } = 16.0;
    public static FontWeight FontWeight { get; set; } = FontWeights.Bold;

    public static Thickness BeveledBorderThickness = new(2);
    public static double BeveledLineWidth = 4;
    
    public static Color ToWindowsColor(System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
