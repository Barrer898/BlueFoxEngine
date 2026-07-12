using Raylib_cs;
namespace BlueFoxEngine.Helper;

public static class Text
{
    public static int MeasureTextWidth(string text, int fontSize) => Raylib.MeasureText(text, fontSize);
 
}