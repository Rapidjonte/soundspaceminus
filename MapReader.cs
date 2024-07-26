using Raylib_cs;
using System.Globalization;
using System.Collections.Generic;
using Rhythia.Content.Beatmaps;

public static class MapReader
{
    public static List<Note> txt(string txtPath)
    {
        string mapData = File.ReadAllText(txtPath);
        List<Note> unspawnedNotes = new List<Note>();
        string songName = mapData.Split(',')[0];
        string[] splitData = mapData.Replace(songName + ",", "").Split(',', '|');
        int dataType = 0;
        float x = 0;
        float y = 0;
        float ms = 0;
        int colorIndex = 0;
        foreach (string data in splitData)
        {
            if (dataType == 0)
            {
                x = float.Parse(data, CultureInfo.InvariantCulture);
                dataType++;
            }
            else if (dataType == 1)
            {
                y = float.Parse(data, CultureInfo.InvariantCulture);
                dataType++;
            }
            else if (dataType == 2)
            {
                ms = float.Parse(data, CultureInfo.InvariantCulture) + Program.noteOffset;
                dataType = 0;
                unspawnedNotes.Add(new Note { X = x, Y = y, Time = ms, Color = Program.colorList[colorIndex] });
                colorIndex = (colorIndex == Program.colorList.Length - 1) ? 0 : colorIndex + 1;
            }
        }
        Console.WriteLine("map loaded!");
        return unspawnedNotes;
    }
    
    public static List<Note> sspm(string sspmPath)
    {
        IBeatmapSet map = new SSPMap(sspmPath);
        List <Note> unspawnedNotes = new List<Note>();
        int colorIndex = 0;
        foreach (Note note in map.Difficulties[0].Notes)
        {
            unspawnedNotes.Add(new Note() { X = note.X+1, Y=note.Y + 1, Time = note.Time*1000+Program.noteOffset, Color = Program.colorList[colorIndex] });
            colorIndex = (colorIndex == Program.colorList.Length - 1) ? 0 : colorIndex + 1;
        }
        return unspawnedNotes;
    }

    public static string GetFileFormat(byte[] bytes) // made by my.narco
    {
        if (bytes.Length < 10) return "unknown";
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46) return ".wav";
        if ((bytes[0] == 0xFF && (bytes[1] == 0xFB || (bytes[1] == 0xFA && bytes[2] == 0x90))) || (bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33)) return ".mp3";
        if (bytes[0] == 0x4F && bytes[1] == 0x67 && bytes[2] == 0x67 && bytes[3] == 0x53) return ".ogg";
        return "unknown";
    }
}