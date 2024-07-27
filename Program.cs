using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Globalization;
using System;
using System.IO;
using System.Formats.Tar;
using Rhythia.Content.Beatmaps;
using System.ComponentModel.Design;
using soundspaceminus;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

internal partial class Program
{
    static int screenWidth = 2560;
    static int screenHeight = 1440;
    static int FPS = 165;

    static float sens = 1;
    static float cursorSize = 0.2f;
    static float cursorHitboxSize = 0.6f;

    static int fontSize = 50;

    static float noteRoundness = 0.3f;
    static int segments = 1;
    static float noteThickness = 40;
    public static float noteOffset = 999;

    public static float ar = 15;
    public static float sd = 15;
    static float hitWindow = 0.5f;
    static bool doNotePushback = true;

    public static Color[] colorList = { Color.SkyBlue, Color.Pink };

    static bool dead = false;
    static int health = 5;

    static float songOffset = 890;
    static string hitPath = Path.Combine("Resources", "hit.wav");
    static string missPath = Path.Combine("Resources", "miss.wav");
    static string cursorPath = Path.Combine("Resources", "cursor.png");
    static string menuLoopPath = Path.Combine("Resources", "menuLoop.ogg");

    static int misses = 0;
    static int hits = 0;
    static int noteCount = 0;
    static float skippedMilliseconds = 0;
    static bool playSong = true;

    static void Main(string[] args)
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Sound Space Minus");
        Raylib.SetTargetFPS(FPS);
        Raylib.InitAudioDevice();
        Raylib.SetWindowIcon(Raylib.LoadImage(Path.Combine("Resources", "icon.png")));
        Raylib.ToggleBorderlessWindowed();

        Play("sspm", false);

        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    static void Play(string mapFileType, bool localFile)
    {
        misses = 0;
        hits = 0;
        dead = false;
        health = 5;
        skippedMilliseconds = 0;
        playSong = true;

        Sound hit = Raylib.LoadSound(hitPath);
        Sound miss = Raylib.LoadSound(missPath);
        Texture2D cursorTexture = Raylib.LoadTexture(cursorPath);

        List<Note> unspawnedNotes = new List<Note>();
        List<Note> renderedNotes = new List<Note>();

        Stopwatch timer = new Stopwatch();
        Stopwatch pauseTimer = new Stopwatch();

        float grid_size = screenHeight * 0.7f;
        ar /= 1000;
        sd *= 35;
        // pls adjust variables to resolution

        string mapPath = Path.Combine("Resources", "map.txt");
        string songPath = Path.Combine("Resources", "song.mp3");
        string sspmPath = Path.Combine("Resources", "map.sspm");
        Sound song = Raylib.LoadSound(songPath);
        IBeatmapSet map = new SSPMap(sspmPath);
        if (localFile)
        {
            if (mapFileType == "txt")
            {
                mapPath = Path.Combine("Resources", "map.txt"); // you should get to pick
                songPath = Path.Combine("Resources", "song.mp3"); // you should get to pick
                song = Raylib.LoadSound(songPath);
                unspawnedNotes = MapReader.txt(mapPath);
                noteCount = unspawnedNotes.Count;
            }
            else if (mapFileType == "sspm")
            {
                sspmPath = Path.Combine("Resources", "map.sspm"); // you should get to pick
                map = new SSPMap(sspmPath);
                unspawnedNotes = MapReader.sspm(sspmPath);
                noteCount = unspawnedNotes.Count;
                byte[] audioData = new SSPMap(sspmPath).AudioData;
                song = Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(MapReader.GetFileFormat(audioData), audioData));
            }
            else { Console.WriteLine("mapFileType not recognized"); }
        } else
        {
            Sound menuLoop = Raylib.LoadSound(menuLoopPath);
            Texture2D logo = Raylib.LoadTexture(Path.Combine("Resources", "logo.png"));
            float logoAspectRatio = (float)logo.Width / logo.Height;
            float logoWidth, logoHeight;
            if (screenWidth / screenHeight > logoAspectRatio)
            {
                logoHeight = screenHeight * 0.2f;
                logoWidth = logoHeight * logoAspectRatio;
            }
            else
            {
                logoWidth = screenWidth * 0.4f;
                logoHeight = logoWidth / logoAspectRatio;
            }
            float logoX = (screenWidth - logoWidth) / 2;
            float logoY = (screenHeight - logoHeight) / 2 - (int)(screenHeight*0.2);
            while (unspawnedNotes.Count == 0 && !Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                if (!Raylib.IsSoundPlaying(menuLoop))
                {
                    Raylib.PlaySound(menuLoop);
                }

                Raylib.DrawTexturePro(
                    logo,
                    new Rectangle(0, 0, logo.Width, logo.Height),
                    new Rectangle(logoX, logoY, logoWidth, logoHeight),
                    Vector2.Zero,
                    0.0f,
                    Color.White
                );

                DragAndDropHandler.CheckForFileDrop((newNotes, newNoteCount, newSong, newSspmPath) =>
                {
                    Raylib.UnloadSound(song);
                    Raylib.UnloadSound(hit);
                    Raylib.UnloadSound(miss);
                    unspawnedNotes = newNotes;
                    noteCount = newNoteCount;
                    song = newSong;
                    sspmPath = newSspmPath;
                    skippedMilliseconds = 0;
                    pauseTimer.Restart();
                });

                Raylib.EndDrawing();
            }
            Raylib.UnloadSound(menuLoop);
            Raylib.UnloadTexture(logo);
            if (Raylib.WindowShouldClose())
            {
                return;
            }
        }

        Raylib.SetMousePosition(screenWidth / 2, screenHeight / 2);
        Raylib.DisableCursor();
        timer.Start();
        pauseTimer.Start();
        bool paused = false; 
        while (!Raylib.WindowShouldClose() && !(renderedNotes.Count==0&&unspawnedNotes.Count==0))
        {
            DragAndDropHandler.CheckForFileDrop((newNotes, newNoteCount, newSong, newSspmPath) =>
            {
                Raylib.UnloadSound(song);
                unspawnedNotes = newNotes;
                noteCount = newNoteCount;
                song = newSong;
                sspmPath = newSspmPath;
                timer.Restart();
                pauseTimer.Restart();
                playSong = true;
                skippedMilliseconds = 0;
                renderedNotes.Clear();
            });

            if (dead)
            {
                break;
            }

            if (playSong && (timer.Elapsed.TotalMilliseconds + skippedMilliseconds) > songOffset)
            {
                Console.WriteLine("played song");
                Raylib.PlaySound(song);
                playSong = false;
            }

            while (unspawnedNotes.Count > 0 && unspawnedNotes[0].Time < (timer.Elapsed.TotalMilliseconds + skippedMilliseconds) + sd)
            {
                renderedNotes.Add(unspawnedNotes[0]);
                unspawnedNotes.RemoveAt(0);
            }
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            Rectangle borderRect = new Rectangle(screenWidth / 2 - grid_size / 2, screenHeight / 2 - grid_size / 2, grid_size, grid_size);
            bool borderDrawn = false;
            int i = renderedNotes.Count - 1;

            Vector2 mousePosition = Misc.Constraint(Raylib.GetMousePosition() * sens, borderRect);
            Rectangle mouseRect = new Rectangle(
                mousePosition.X - cursorTexture.Width * cursorHitboxSize / 2,
                mousePosition.Y - cursorTexture.Height * cursorHitboxSize / 2,
                cursorTexture.Width * cursorHitboxSize,
                cursorTexture.Height * cursorHitboxSize
            );

            while (i > -1)
            {
                float noteZ = (renderedNotes[i].Time - (float)timer.Elapsed.TotalMilliseconds + skippedMilliseconds) * ar;
                float scale = grid_size / 3.0f / noteZ;
                float thickness = noteThickness / noteZ;
                float adjustedSize = scale - 2 * thickness;
                Rectangle noteRect = new Rectangle(screenWidth / 2 + scale / 2 - renderedNotes[i].X * scale + thickness,
                    screenHeight / 2 + scale / 2 - renderedNotes[i].Y * scale + thickness,
                    adjustedSize,
                    adjustedSize
                );

                if (noteZ <= 1f - hitWindow)
                {
                    misses++;
                    Console.WriteLine("miss");
                    Raylib.PlaySound(miss);
                    renderedNotes.RemoveAt(i);
                    i--;
                    if (health > 1)
                    {
                        health--;
                    } else if (health <= 1)
                    {
                        dead = true;
                    }
                    continue;
                } else if (noteZ <= 1 && Raylib.CheckCollisionRecs(mouseRect, noteRect))
                {
                    hits++;
                    Console.WriteLine("hit");
                    Raylib.PlaySound(hit);
                    renderedNotes.RemoveAt(i);
                    i--;
                    if (health >= 5)
                    {
                        health = 5;
                    }
                    else if ( health < 5)
                    {
                        health++;
                    }
                    continue;
                }

                if (i == 0 && noteZ < 1f && !borderDrawn)
                {
                    Raylib.DrawRectangleLinesEx(borderRect, 2, Color.White);
                    borderDrawn = true;
                }
                if (noteZ > 1f || doNotePushback)
                {
                    Raylib.DrawRectangleRoundedLines(
                        noteRect,
                        noteRoundness,
                        segments,
                        thickness,
                        renderedNotes[i].Color
                    );
                }
                i--;
            }
            if (renderedNotes.Count == 0 || !borderDrawn)
            {
                Raylib.DrawRectangleLinesEx(borderRect, 2, Color.White);
            }

            Raylib.DrawText("FPS: " + Raylib.GetFPS(), 0, 0, fontSize, Color.White);

            if (renderedNotes.Count == 0 && unspawnedNotes.Count != 0 && unspawnedNotes[0].Time > (timer.Elapsed.TotalMilliseconds + skippedMilliseconds) + sd + 4000)
            {
                Raylib.DrawText("Press SPACE to skip", screenWidth / 2 - (int)grid_size / 2, screenHeight / 2 - (int)grid_size / 2-fontSize, fontSize, Color.White);
                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    // SKIP IS NOT IMPLEMENTED YET
                    
                }
            } 
            else if (Raylib.IsKeyPressed(KeyboardKey.Space) && !paused && pauseTimer.Elapsed.TotalSeconds > 1)
            {
                Raylib.PauseSound(song);
                timer.Stop();
                pauseTimer.Reset();
                paused = true;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Space) && paused)
            {
                Raylib.ResumeSound(song);
                timer.Start();
                pauseTimer.Start();
                paused = false;
            }

            Raylib.DrawTextureEx(cursorTexture, new Vector2(mousePosition.X - cursorTexture.Width*cursorSize / 2, mousePosition.Y - cursorTexture.Height*cursorSize / 2), 0, cursorSize, Color.White);

            if (Raylib.IsKeyDown(KeyboardKey.One))
            {
                Raylib.DrawText("debug view", 0, 500, 100, Color.Red);
                Raylib.DrawText("health: " + health, 0, 600, 50, Color.Red);
                Raylib.DrawText("pauseTimer: " + pauseTimer.ElapsedMilliseconds, 0, 650, 50, Color.Red);
                Raylib.DrawRectangle((int)mouseRect.X, (int)mouseRect.Y, (int)mouseRect.Width, (int)mouseRect.Height, Color.Red);
                Raylib.DrawText($"Misses: {misses}\n\n\n\nAccuracy: {(float)(hits / (float)(misses + hits)) * 100}%\n\n\n\nProgress: {timer.ElapsedMilliseconds / (map.Difficulties[0].Notes[map.Difficulties[0].Notes.Length - 1].Time*1000)*100}%", 0, 250, 50, Color.Red);
                Raylib.DrawText((int)Math.Round(map.Difficulties[0].Notes[map.Difficulties[0].Notes.Length - 1].Time) + " seconds - " + map.Difficulties[0].Notes.Length + " notes", 0, 50, 50, Color.White);
                Raylib.DrawText(map.Artist + " - " + map.Title, 0, 100, 50, Color.White);
                Raylib.DrawText("Mappers: " + string.Join(", ", map.Mappers), 0, 150, 50, Color.White);
            }
            Raylib.EndDrawing();
        }
        timer.Stop();
        Raylib.EnableCursor();
        if (dead && mapFileType == "sspm")
        {
            IBeatmapSet watMap = new SSPMap(sspmPath);
            Raylib.UnloadSound(song);
            Console.WriteLine($"hits: {hits}\nmisses: {misses}\nseconds: {watMap.Difficulties[0].Notes[watMap.Difficulties[0].Notes.Length - 1].Time}\nnoteCount: {noteCount}\nnotesReached: {misses + hits}\naccuracy: {(float)hits / (float)(misses + hits) * 100}%\nProgress: {(short)((float)(hits + misses) / (float)noteCount * 100)}%");
            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                Raylib.DrawText((int)Math.Round(watMap.Difficulties[0].Notes[watMap.Difficulties[0].Notes.Length - 1].Time) + " seconds - " + watMap.Difficulties[0].Notes.Length + " notes", 0, 0, 50, Color.White);
                Raylib.DrawText(watMap.Artist + " - " + watMap.Title, 0, 0 + 60, 100, Color.White);
                Raylib.DrawText("Mappers: " + string.Join(", ", watMap.Mappers), 0, 100 + 60, 50, Color.White);

                
                Raylib.DrawText($"Misses: {misses}\n\n\n\nAccuracy: {(float)(hits / (float)(misses + hits)) * 100}%\n\n\n\nProgress: {timer.ElapsedMilliseconds / (map.Difficulties[0].Notes[map.Difficulties[0].Notes.Length - 1].Time * 1000) * 100}%", 0, 180 + 60+60, 50, Color.White);
                Raylib.DrawText("YOU FAILED", 0, 600, 100, Color.White);

                Raylib.EndDrawing();
            }
        }
        else if (!dead && mapFileType == "sspm")
        {
            IBeatmapSet watMap = new SSPMap(sspmPath);
            Console.WriteLine($"hits: {hits}\nmisses: {misses}\nseconds: {watMap.Difficulties[0].Notes[watMap.Difficulties[0].Notes.Length - 1].Time}\nnoteCount: {noteCount}\nnotesReached: {misses + hits}\naccuracy: {(float)hits / (float)(misses + hits) * 100}%\nProgress: {(short)((float)(hits + misses) / (float)noteCount * 100)}%");
            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                Raylib.DrawText((int)Math.Round(watMap.Difficulties[0].Notes[watMap.Difficulties[0].Notes.Length - 1].Time) + " seconds - " + watMap.Difficulties[0].Notes.Length + " notes", 0, 0, 50, Color.White);
                Raylib.DrawText(watMap.Artist + " - " + watMap.Title, 0, 0 + 60, 100, Color.White);
                Raylib.DrawText("Mappers: " + string.Join(", ", watMap.Mappers), 0, 100 + 60, 50, Color.White);

                Raylib.DrawText($"Misses: {misses}\n\n\n\nAccuracy: {(short)((float)(hits / (float)(misses + hits)) * 100)}%\n\n\n\nProgress: {(short)((float)(hits + misses) / (float)noteCount * 100)}%", 0, 180 + 60 + 60, 50, Color.White);
                Raylib.DrawText("YOU PASSED", 0, 600, 100, Color.White);
                Raylib.EndDrawing();
            }
        }
    }
}