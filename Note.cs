namespace Rhythia.Content.Beatmaps;
using Raylib_cs;

public struct Note {
    public float X;
    public float Y;
    public float Time;
    public Color Color;
}
public class Beatmap
{
    public string Name = "";
    public Note[] Notes = [];
}