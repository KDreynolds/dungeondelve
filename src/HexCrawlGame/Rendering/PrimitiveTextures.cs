using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexCrawlGame;

public static class PrimitiveTextures
{
    public static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }

    public static Texture2D CreateIsoDiamond(GraphicsDevice graphicsDevice, int width, int height)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var data = new Color[width * height];

        float halfW = width / 2f;
        float halfH = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = MathF.Abs(x - halfW + 0.5f);
                float dy = MathF.Abs(y - halfH + 0.5f);
                bool inside = (dx / halfW + dy / halfH) <= 1f;
                data[y * width + x] = inside ? Color.White : Color.Transparent;
            }
        }

        texture.SetData(data);
        return texture;
    }

    public static Texture2D CreateHexMask(GraphicsDevice graphicsDevice, float size)
    {
        int width = (int)MathF.Ceiling(MathF.Sqrt(3f) * size) + 2;
        int height = (int)MathF.Ceiling(2f * size) + 2;

        var texture = new Texture2D(graphicsDevice, width, height);
        var data = new Color[width * height];
        var center = new Vector2(width / 2f, height / 2f);
        var corners = HexMath.GetHexCorners(center, size);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                data[y * width + x] = PointInPolygon(point, corners) ? Color.White : Color.Transparent;
            }
        }

        texture.SetData(data);
        return texture;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            bool intersect = (polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)
                && point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y)
                / (polygon[j].Y - polygon[i].Y) + polygon[i].X;

            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
