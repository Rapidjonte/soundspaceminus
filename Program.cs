using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Globalization;
using System;
using System.IO;
using System.Formats.Tar;
using Rhythia.Content.Beatmaps;

internal partial class Program
{
    static int screenWidth = 2560;
    static int screenHeight = 1440;
    static int FPS = 165;

    static float noteRoundness = 0.5f;
    static int segments = 10;
    static float noteThickness = 35;
    public static float noteOffset = 999;

    static float ar = 25;
    static float sd = 15;
    static float hitWindow = 0.3f;
    static bool doNotePushback = false;

    public static Color[] colorList = { Color.Pink, Color.SkyBlue };

    static bool dead = false;

    static float songOffset = 799;
    static string hitPath = Path.Combine("Resources", "hit.wav");
    static string missPath = Path.Combine("Resources", "miss.wav");
    static string sspmPath = Path.Combine("Resources", "map.sspm");

    static void Main(string[] args)
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Sound Space Minus");
        Raylib.SetTargetFPS(FPS);
        Raylib.InitAudioDevice();
        Raylib.SetWindowIcon(Raylib.LoadImage(Path.Combine("Resources", "icon.png")));
        Raylib.ToggleBorderlessWindowed();

        LoadMapAndPlay("sspm");

        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    static void LoadMapAndPlay(string mapFileType)
    {
        Sound hit = Raylib.LoadSound(hitPath);
        Sound miss = Raylib.LoadSound(missPath);

        List<Note> unspawnedNotes = new List<Note>();
        List<Note> renderedNotes = new List<Note>();

        Stopwatch timer = new Stopwatch();
        bool playSong = true;

        float grid_size = screenHeight * 0.7f;
        ar /= 1000;
        sd *= 35;

        string mapPath = Path.Combine("Resources", "map.txt");
        string songPath = Path.Combine("Resources", "song.mp3");
        string sspmPath;
        Sound song = Raylib.LoadSound(songPath);
        IBeatmapSet map;
        if (mapFileType == "txt")
        {
            mapPath = Path.Combine("Resources", "map.txt"); // you should get to pick
            songPath = Path.Combine("Resources", "song.mp3"); // you should get to pick
            song = Raylib.LoadSound(songPath);
            unspawnedNotes = MapReader.txt(mapPath);
        } else if (mapFileType == "sspm")
        {
            sspmPath = @"C:\Users\Rapid\Downloads\map.sspm";
            map = new SSPMap(sspmPath);
            unspawnedNotes = MapReader.sspm(sspmPath);
            byte[] audioData = new SSPMap(sspmPath).AudioData;
            song = Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(MapReader.GetFileFormat(audioData), audioData));
            songOffset += 122;
        }

        timer.Start();

        while (!Raylib.WindowShouldClose())
        {
            if (playSong && timer.Elapsed.TotalMilliseconds > songOffset)
            {
                Console.WriteLine("played song");
                Raylib.PlaySound(song);
                playSong = false;
            }

            while (unspawnedNotes.Count > 0 && unspawnedNotes[0].Time < timer.Elapsed.TotalMilliseconds + sd)
            {
                renderedNotes.Add(unspawnedNotes[0]);
                unspawnedNotes.RemoveAt(0);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawRectangleLinesEx(new Rectangle(screenWidth / 2 - grid_size / 2, screenHeight / 2 - grid_size / 2, grid_size, grid_size), 2, Color.White);
            Raylib.DrawText("FPS: " + Raylib.GetFPS(), 0, 0, 100, Color.White);
            if (renderedNotes.Count > 0)
                Raylib.DrawText(renderedNotes[0].X.ToString(), 0, 100, 50, Color.White);

            for (int i = renderedNotes.Count - 1; i > -1; i--)
            {
                float noteZ = (renderedNotes[i].Time - (float)timer.Elapsed.TotalMilliseconds) * ar;
                if (noteZ <= 1f - hitWindow)
                {
                    Raylib.PlaySound(miss);
                    renderedNotes.RemoveAt(i);
                    i--;
                    continue;
                }

                float scale = grid_size / 3.0f / noteZ;
                float thickness = noteThickness / noteZ;
                float adjustedSize = scale - 2 * thickness;

                if (noteZ > 1f || doNotePushback)
                {
                    Raylib.DrawRectangleRoundedLines(
                        new Rectangle(
                            screenWidth / 2 + scale / 2 - renderedNotes[i].X * scale + thickness,
                            screenHeight / 2 + scale / 2 - renderedNotes[i].Y * scale + thickness,
                            adjustedSize,
                            adjustedSize
                        ),
                        noteRoundness,
                        segments,
                        thickness,
                        renderedNotes[i].Color
                    );
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.UnloadSound(song);
        Raylib.UnloadSound(hit);
        Raylib.UnloadSound(miss);
    }
}