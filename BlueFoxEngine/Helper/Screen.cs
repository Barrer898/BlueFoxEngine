using Raylib_cs;
namespace BlueFoxEngine.Helper;

public static class Screen
{
    public static int Width => Raylib.GetScreenWidth();
    public static int Height => Raylib.GetScreenHeight();
}