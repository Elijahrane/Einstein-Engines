using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Shared.DeltaV.Arcade.S3D;
using Color = Robust.Shared.Maths.Color;
using Robust.Client.ResourceManagement;
using Content.Client.Resources;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.DeltaV.Arcade.S3D.Renderer;

/// <summary>
/// Controls the rendering for the S3D arcade. Logic is in the client's S3DArcadeSystem.cs
/// </summary>
public sealed class S3DRenderer : Control
{
    private readonly IResourceCache _resourceCache;

    /// <summary>
    /// Buffer for walls.
    /// </summary>
    private DrawVertexUV2DColor[] _buffer = Array.Empty<DrawVertexUV2DColor>();
    private S3DArcadeComponent _comp;
    private int[,] _worldMap;
    private readonly Vector3[,] _wallAtlas;
    public S3DRenderer(IResourceCache resourceCache, S3DArcadeComponent comp, int[,] worldMap, Vector3[,] wallAtlas)
    {
        _resourceCache = resourceCache;
        _comp = comp;
        _worldMap = worldMap;
        _wallAtlas = wallAtlas;
    }
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // TODO: As game logic is locked to 30 fps, we don't need to draw if there hasn't been a frame update.

        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        Raycast();
        watch.Stop();
        Logger.Error("Raycasted in " + watch.ElapsedMilliseconds + " ms");

        Logger.Error("Drawing " + _buffer.Length + " points.");

        // There's a size limit of 65532 elements.
        int i = 0;
        while (i < _buffer.Length)
        {
            if (_buffer.Length > i + 65530)
                handle.DrawPrimitives(DrawPrimitiveTopology.PointList, Texture.White, _buffer.AsSpan(i, 65530));
            else
                handle.DrawPrimitives(DrawPrimitiveTopology.PointList, Texture.White, _buffer.AsSpan(i));

            i += 65531;
        }
    }
    private void Raycast()
    {
        // a lot of this is adapted from https://lodev.org/cgtutor/raycasting.html (which is BSD licensed.) Thank you Lode Vandevenne.

        List<DrawVertexUV2DColor> verts = new List<DrawVertexUV2DColor>();
        for (int x = 0; x < Size.X; x++)
        {
            double cameraX = 2 * (double) x / Size.X - 1; //x-coordinate in camera space
            double rayDirX = _comp.State.DirX + _comp.State.PlaneX * cameraX;
            double rayDirY = _comp.State.DirY + _comp.State.PlaneY * cameraX;

            //which box of the map we're in
            int mapX = (int) _comp.State.PosX;
            int mapY = (int) _comp.State.PosY;

            //length of ray from current position to next x or y-side
            double sideDistX;
            double sideDistY;

            //length of ray from one x or y-side to next x or y-side
            //these are derived as:
            //deltaDistX = sqrt(1 + (rayDirY * rayDirY) / (rayDirX * rayDirX))
            //deltaDistY = sqrt(1 + (rayDirX * rayDirX) / (rayDirY * rayDirY))
            //which can be simplified to abs(|rayDir| / rayDirX) and abs(|rayDir| / rayDirY)
            //where |rayDir| is the length of the vector (rayDirX, rayDirY). Its length,
            //unlike (dirX, dirY) is not 1, however this does not matter, only the
            //ratio between deltaDistX and deltaDistY matters, due to the way the DDA
            //stepping further below works. So the values can be computed as below.
            double deltaDistX = Math.Abs(1 / rayDirX);
            double deltaDistY = Math.Abs(1 / rayDirY);

            double perpWallDist;

            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;

            int hit = 0; //was there a wall hit?
            bool side = false; //was a NS or a EW wall hit?
            //calculate step and initial sideDist
            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (float) (_comp.State.PosX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (float) (mapX + 1.0 - _comp.State.PosX) * deltaDistX;
            }
            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (float) (_comp.State.PosY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1.0 - _comp.State.PosY) * deltaDistY;
            }
            //perform DDA
            while (hit == 0)
            {
                //jump to next map square, either in x-direction, or in y-direction
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = false;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = true;
                }
                //Check if ray has hit a wall
                if (_worldMap[mapX, mapY] > 0)
                {
                    hit = 1;
                }
            }

            if (!side)
                perpWallDist = sideDistX - deltaDistX;
            else
                perpWallDist = sideDistY - deltaDistY;

            float lineHeight = (float) (Size.Y / perpWallDist);

            float drawStart = -lineHeight / 2 + Size.Y / 2;
            if (drawStart < 0)
                drawStart = 0;

            double wallX; //where exactly the wall was hit
            if (!side)
                wallX = _comp.State.PosY + perpWallDist * rayDirY;
            else
                wallX = _comp.State.PosX + perpWallDist * rayDirX;
            wallX -= Math.Floor(wallX); // this leaves just the remainder

            Color color = Color.Pink;
            // Logger.Error("Pink R: " + color.R);
            // Logger.Error("Ping G: " + color.G);
            // Logger.Error("Pink B: " + color.B);

            int i = 0;
            while (i < lineHeight)
            {
                var ratio = (float) i / lineHeight;
                var rgb = _wallAtlas[(int) Math.Clamp(wallX * 64, 1, Size.X), Math.Clamp((int) (64 * ratio), 1, 64)];

                color.R = rgb.X;
                color.G = rgb.Y;
                color.B = rgb.Z;
                if (side)
                    color = Color.FromSrgb(new Color(color.R / 2, color.G / 2, color.B / 2, 1));
                verts.Add(new DrawVertexUV2DColor(new Vector2(x + 1, drawStart + i), color)); // x
                i++;
            }
        }
        _buffer = verts.ToArray();
    }
}
