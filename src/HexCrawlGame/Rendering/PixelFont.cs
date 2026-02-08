using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexCrawlGame;

public sealed class PixelFont
{
    public int GlyphWidth => 5;
    public int GlyphHeight => 7;
    public int Spacing => 1;
    public int LineSpacing => 2;

    private static readonly Dictionary<char, byte[]> Glyphs = new()
    {
        ['A'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['B'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 },
        ['C'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 },
        ['D'] = new byte[] { 0b11100, 0b10010, 0b10001, 0b10001, 0b10001, 0b10010, 0b11100 },
        ['E'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
        ['F'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['G'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01110 },
        ['H'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['I'] = new byte[] { 0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['J'] = new byte[] { 0b00111, 0b00010, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100 },
        ['K'] = new byte[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 },
        ['L'] = new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 },
        ['M'] = new byte[] { 0b10001, 0b11011, 0b10101, 0b10001, 0b10001, 0b10001, 0b10001 },
        ['N'] = new byte[] { 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001 },
        ['O'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['P'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['Q'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 },
        ['R'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 },
        ['S'] = new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 },
        ['T'] = new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['U'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['V'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
        ['W'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10101, 0b11011, 0b10001 },
        ['X'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001 },
        ['Y'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['Z'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111 },
        ['0'] = new byte[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110 },
        ['1'] = new byte[] { 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['2'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111 },
        ['3'] = new byte[] { 0b11110, 0b00001, 0b00001, 0b01110, 0b00001, 0b00001, 0b11110 },
        ['4'] = new byte[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010 },
        ['5'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b00001, 0b00001, 0b11110 },
        ['6'] = new byte[] { 0b01110, 0b10000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110 },
        ['7'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000 },
        ['8'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 },
        ['9'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00001, 0b01110 },
        [':'] = new byte[] { 0b00000, 0b00100, 0b00100, 0b00000, 0b00100, 0b00100, 0b00000 },
        ['.'] = new byte[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00100, 0b00100 },
        ['-'] = new byte[] { 0b00000, 0b00000, 0b00000, 0b11111, 0b00000, 0b00000, 0b00000 },
        ['+'] = new byte[] { 0b00000, 0b00100, 0b00100, 0b11111, 0b00100, 0b00100, 0b00000 },
        ['/'] = new byte[] { 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b00000, 0b00000 },
        ['?'] = new byte[] { 0b01110, 0b10001, 0b00010, 0b00100, 0b00100, 0b00000, 0b00100 },
        [' '] = new byte[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000 }
    };

    public Point MeasureString(string text, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Point.Zero;
        }

        int maxChars = 0;
        int currentChars = 0;
        int lines = 1;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                maxChars = Math.Max(maxChars, currentChars);
                currentChars = 0;
                lines++;
                continue;
            }

            currentChars++;
        }

        maxChars = Math.Max(maxChars, currentChars);

        int width = maxChars > 0
            ? (maxChars * (GlyphWidth + Spacing) - Spacing) * scale
            : 0;
        int height = lines * GlyphHeight * scale + (lines - 1) * LineSpacing * scale;

        return new Point(width, height);
    }

    public void DrawString(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, Color color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float startX = position.X;
        float x = position.X;
        float y = position.Y;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                x = startX;
                y += (GlyphHeight + LineSpacing) * scale;
                continue;
            }

            DrawChar(spriteBatch, pixel, char.ToUpperInvariant(c), new Vector2(x, y), color, scale);
            x += (GlyphWidth + Spacing) * scale;
        }
    }

    private void DrawChar(SpriteBatch spriteBatch, Texture2D pixel, char c, Vector2 position, Color color, int scale)
    {
        if (!Glyphs.TryGetValue(c, out var glyph))
        {
            glyph = Glyphs['?'];
        }

        for (int row = 0; row < GlyphHeight; row++)
        {
            byte rowBits = glyph[row];
            for (int col = 0; col < GlyphWidth; col++)
            {
                int mask = 1 << (GlyphWidth - 1 - col);
                if ((rowBits & mask) == 0)
                {
                    continue;
                }

                var rect = new Rectangle(
                    (int)position.X + col * scale,
                    (int)position.Y + row * scale,
                    scale,
                    scale);
                spriteBatch.Draw(pixel, rect, color);
            }
        }
    }
}
