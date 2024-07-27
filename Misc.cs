using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Raylib_cs;
using Rhythia.Content.Beatmaps;

namespace soundspaceminus
{
    internal class Misc
    {
        public static Vector2 Constraint(Vector2 mousePosition, Rectangle borderRect)
        {
            if (mousePosition.X < borderRect.X)
            {
                mousePosition.X = (int)borderRect.X;
            }
            else if (mousePosition.X > borderRect.X + borderRect.Width)
            {
                mousePosition.X = (int)borderRect.X + borderRect.Width;
            }
            if (mousePosition.Y < borderRect.Y)
            {
                mousePosition.Y = (int)borderRect.Y;
            }
            else if (mousePosition.Y > borderRect.Y + borderRect.Height)
            {
                mousePosition.Y = (int)borderRect.Y + borderRect.Height;
            }
            Raylib.SetMousePosition((int)mousePosition.X, (int)mousePosition.Y);
            return mousePosition;
        }
        public static void DrawCenteredText(string text, int screenWidth, int screenHeight, int fontSize, Color color)
        {
            int textWidth = Raylib.MeasureText(text, fontSize);
            int textHeight = fontSize;

            int x = (screenWidth - textWidth) / 2;
            int y = (screenHeight - textHeight) / 2;

            Raylib.DrawText(text, x, y, fontSize, color);
        }

        public static void DrawCenteredTextInRect(string text, Rectangle rect, int fontSize, Color color)
        {
            int textWidth = Raylib.MeasureText(text, fontSize);
            int textHeight = fontSize; 

            int x = (int)(rect.X + (rect.Width - textWidth) / 2);
            int y = (int)(rect.Y + (rect.Height - textHeight) / 2);

            Raylib.DrawText(text, x, y, fontSize, color);
        }
    }
}