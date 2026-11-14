namespace KantinKita.Helpers;

public static class BrushFactory
{
    public static Brush Gradient(string startHex, string endHex) => new LinearGradientBrush
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 1),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(startHex), 0f),
            new GradientStop(Color.FromArgb(endHex), 1f),
        },
    };
}
