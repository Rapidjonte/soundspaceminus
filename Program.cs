using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Globalization;
using System;
using System.IO;

internal class Program
{
    static int screenWidth = 1920;
    static int screenHeight = 1080;
    static int FPS = 60;

    static float noteRoundness = 0.5f;
    static int segments = 10;
    static float noteThickness = 35;
    static float noteOffset = 999;

    static float ar = 25;
    static float sd = 15;
    static float hitWindow = 0.3f;
    static bool doNotePushback = false;

    static bool dead = false;

    static float songOffset = 888;
    static string songPath = Path.Combine("Resources", "song.mp3");
    static string hitPath = Path.Combine("Resources", "hit.wav");
    static string missPath = Path.Combine("Resources", "miss.wav");
    static string mapPath = Path.Combine("Resources", "map.txt");

    static void Main(string[] args)
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Sound Space Minus");
        Raylib.SetTargetFPS(FPS);
        Raylib.InitAudioDevice();

        LoadMapAndPlay();

        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    static void LoadMapAndPlay()
    {
        Sound song = Raylib.LoadSound(songPath);
        Sound hit = Raylib.LoadSound(hitPath);
        Sound miss = Raylib.LoadSound(missPath);

        List<Note> unspawnedNotes = new List<Note>();
        List<Note> renderedNotes = new List<Note>();

        Stopwatch timer = new Stopwatch();
        bool playSong = true;

        float grid_size = screenHeight * 0.7f;
        ar /= 1000;
        sd *= 35;

        string mapData = File.ReadAllText(mapPath);
        string songName = mapData.Split(',')[0];
        string[] splitData = mapData.Replace(songName + ",", "").Split(',', '|');
        int dataType = 0;
        float x = 0;
        float y = 0;
        float ms = 0;
        Color[] colorList = { Color.Purple, Color.Blue };
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
                ms = float.Parse(data, CultureInfo.InvariantCulture) + noteOffset;
                dataType = 0;
                unspawnedNotes.Add(new Note { X = x, Y = y, Ms = ms, Color = colorList[colorIndex] });
                colorIndex = (colorIndex == colorList.Length - 1) ? 0 : colorIndex + 1;
            }
        }
        Console.WriteLine("map loaded!");

        timer.Start();
        while (!Raylib.WindowShouldClose())
        {
            if (playSong && timer.Elapsed.TotalMilliseconds > songOffset)
            {
                Raylib.PlaySound(song);
                playSong = false;
            }

            while (unspawnedNotes.Count > 0 && unspawnedNotes[0].Ms < timer.Elapsed.TotalMilliseconds + sd)
            {
                renderedNotes.Add(unspawnedNotes[0]);
                unspawnedNotes.RemoveAt(0);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawRectangleLinesEx(new Rectangle(screenWidth / 2 - grid_size / 2, screenHeight / 2 - grid_size / 2, grid_size, grid_size), 2, Color.White);
            Raylib.DrawText("FPS: " + Raylib.GetFPS(), 0, 0, 100, Color.White);

            for (int i = renderedNotes.Count - 1; i > -1; i--)
            {
                renderedNotes[i].Z = (renderedNotes[i].Ms - (float)timer.Elapsed.TotalMilliseconds) * ar;
                if (renderedNotes[i].Z <= 1f - hitWindow)
                {
                    Raylib.PlaySound(hit);
                    renderedNotes.RemoveAt(i);
                    i--;
                    continue;
                }

                float scale = grid_size / 3.0f / renderedNotes[i].Z;
                float thickness = noteThickness / renderedNotes[i].Z;
                float adjustedSize = scale - 2 * thickness;

                if (renderedNotes[i].Z > 1f || doNotePushback)
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

    class Note
    {
        public float X;
        public float Y;
        public float Z;
        public float Ms;
        public Color Color;
    }
}
