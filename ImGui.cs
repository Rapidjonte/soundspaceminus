using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;
using soundspaceminus;
using Rhythia.Content.Beatmaps;

internal class DragAndDropHandler
{
    public static void CheckForFileDrop(Action<List<Note>, int, Sound, string> LoadMap)
    {
        if (Raylib.IsFileDropped())
        {
            string[] droppedFiles = Raylib.GetDroppedFiles();
            if (droppedFiles.Length > 0 && Path.GetExtension(droppedFiles[0]).Equals(".sspm", StringComparison.OrdinalIgnoreCase))
            {
                string sspmPath = droppedFiles[0];
                IBeatmapSet map = new SSPMap(sspmPath);
                List<Note> unspawnedNotes = MapReader.sspm(sspmPath);
                int noteCount = unspawnedNotes.Count;
                byte[] audioData = new SSPMap(sspmPath).AudioData;
                Sound song = Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(MapReader.GetFileFormat(audioData), audioData));

                LoadMap(unspawnedNotes, noteCount, song, sspmPath);
            }
        }
    }
}
