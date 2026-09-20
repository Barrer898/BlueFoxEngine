using Raylib_cs;

namespace BlueFoxEngine.Helper;

/// <summary>
/// Text measurement utilities built on top of Raylib's text functions.
/// </summary>
public static class Text
{
    /// <summary>
    /// Measures the pixel width of a string at the given font size.
    /// </summary>
    public static int MeasureTextWidth(string text, int fontSize) => Raylib.MeasureText(text, fontSize);
}
