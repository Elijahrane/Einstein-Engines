using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Shared.DeltaV.Arcade.S3D;
using Color = Robust.Shared.Maths.Color;
using Robust.Client.ResourceManagement;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Utility;

namespace Content.Client.DeltaV.Arcade.S3D.Renderer;

/// <summary>
/// Controls the rendering for the S3D arcade. Logic is in the client's S3DArcadeSystem.cs
/// </summary>
public sealed class S3DRenderer : Control
{
    private readonly IResourceCache _resourceCache;
    private const int InternalResX = 320;
    private const int InternalResY = 240;
    private DrawVertexUV2DColor[] _buffer = Array.Empty<DrawVertexUV2DColor>();
    private S3DArcadeComponent _comp;
    private int[,] _worldMap;
    private readonly Image<Rgba32> _wallAtlas;
    private readonly Image<Rgba32> _floorAtlas;
    private readonly Image<Rgba32> _ceilingAtlas;
    private readonly Image<Rgba32> _skybox;
    private long _tick = 0;
    public S3DRenderer(IResourceCache resourceCache, S3DArcadeComponent comp, int[,] worldMap, Image<Rgba32> wallAtlas, Image<Rgba32> floorAtlas, Image<Rgba32> ceilingAtlas, Image<Rgba32> skybox)
    {
        _resourceCache = resourceCache;
        _comp = comp;
        _worldMap = worldMap;
        _wallAtlas = wallAtlas;
        _floorAtlas = floorAtlas;
        _ceilingAtlas = ceilingAtlas;
        _skybox = skybox;
    }
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_comp.State.Tick > _tick)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Raycast();
            watch.Stop();
            if (watch.ElapsedMilliseconds > 1000 / 30)
            {
                Logger.Warning("Over target! Raycasted in " + watch.ElapsedMilliseconds + " ms");
            }
        }

        _tick = _comp.State.Tick;

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
        Color color;
        Vector2 vec = Vector2.One;

        var wallSpan = _wallAtlas.GetPixelSpan();
        var floorSpan = _floorAtlas.GetPixelSpan();
        var ceilingSpan = _ceilingAtlas.GetPixelSpan();
        var skyboxSpan = _skybox.GetPixelSpan();

        var scrollFactor = (float) (new Vector2((float) _comp.State.DirX, (float) _comp.State.DirY).ToAngle().Degrees + 180f) / 360f;
        scrollFactor -= (float) Math.Floor(scrollFactor);

        int scaleFactor = 2;
        List<DrawVertexUV2DColor> verts = new List<DrawVertexUV2DColor>();

        // Skybox
        for (int y = 0; y < InternalResY / 2; y++)
        {
            for (int x = 0; x < InternalResX; x++)
            {
                // Scale X by FOV
                var texX = (int) (x * (66f / 360f) + (int) ((1 - scrollFactor) * 320)) % 320;

                var rgb = skyboxSpan[texX + 320 * y];

                color = new Color(rgb.R, rgb.G, rgb.B);

                int scaleIncrementor = 1;
                while (scaleIncrementor <= scaleFactor * 2) // 2 dimensions, so *2
                {
                    vec.X = (x + 1) * scaleFactor + ((int) Math.Ceiling((double) scaleIncrementor / scaleFactor) - 1); // 0 0 1 1; 0 0 0 1 1 1 2 2 2; etc.
                    vec.Y = y * scaleFactor + scaleIncrementor % scaleFactor; // 1 0 1 0; 1 2 0 1 2 0 1 2 0; etc.

                    verts.Add(new DrawVertexUV2DColor(vec, color));

                    scaleIncrementor++;
                }
            }
        }

        // Floor Casting
        for (int y = InternalResY / 2; y < InternalResY; y++)
        {
            float rayDirX0 = (float) (_comp.State.DirX - _comp.State.PlaneX);
            float rayDirY0 = (float) (_comp.State.DirY - _comp.State.PlaneY);
            float rayDirX1 = (float) (_comp.State.DirX + _comp.State.PlaneX);
            float rayDirY1 = (float) (_comp.State.DirY + _comp.State.PlaneY);

            // Y-coordinates of the center of the screen
            float screenCenter = InternalResY / 2;

            // Difference between current row and screen center
            var yPos = y - screenCenter;

            // horizontal difference between the camera and floor
            float distFloor = yPos == 0 ? 0f : screenCenter / yPos;

            float floorStepX = distFloor * (rayDirX1 - rayDirX0) / InternalResX;
            float floorStepY = distFloor * (rayDirY1 - rayDirY0) / InternalResX;

            float floorX = (float) (_comp.State.PosX + distFloor * rayDirX0);
            float floorY = (float) (_comp.State.PosY + distFloor * rayDirY0);

            for (int x = 0; x < InternalResX; x++)
            {
                int texX = (int) (32 * (floorX - Math.Floor(floorX)) + 1);
                int texY = (int) (32 * (floorY - Math.Floor(floorY)) + 1);

                floorX += floorStepX;
                floorY += floorStepY;

                int scaleIncrementor = 1;
                while (scaleIncrementor <= scaleFactor * 2) // 2 dimensions, so *2
                {
                    var rgbF = floorSpan[texX + 32 * (texY - 1) - 1];
                    color = new Color(rgbF.R, rgbF.G, rgbF.B);
                    vec.X = (x + 1) * scaleFactor + ((int) Math.Ceiling((double) scaleIncrementor / scaleFactor) - 1); // 0 0 1 1; 0 0 0 1 1 1 2 2 2; etc.
                    vec.Y = y * scaleFactor + scaleIncrementor % scaleFactor; // 1 0 1 0; 1 2 0 1 2 0 1 2 0; etc.

                    if (vec.Y > 0 && vec.Y < InternalResY * scaleFactor)
                        verts.Add(new DrawVertexUV2DColor(vec, color));

                    // Readd after wall / ceiling tile support.
                    // var rgbC = ceilingSpan[texX + 32 * (texY - 1) - 1];
                    // color = new Color(rgbC.R, rgbC.G, rgbC.B);
                    // vec.X = (x + 1) * scaleFactor + ((int) Math.Ceiling((double) scaleIncrementor / scaleFactor) - 1); // 0 0 1 1; 0 0 0 1 1 1 2 2 2; etc.
                    // vec.Y = (InternalResY - y) * scaleFactor + scaleIncrementor % scaleFactor; // 1 0 1 0; 1 2 0 1 2 0 1 2 0; etc.

                    // if (vec.Y > 0 && vec.Y < InternalResY * scaleFactor)
                    //     verts.Add(new DrawVertexUV2DColor(vec, color));

                    scaleIncrementor++;
                }
            }

        }

        // Wall Casting
        for (int x = 0; x < InternalResX; x++)
        {
            double cameraX = 2 * (double) x / InternalResX - 1; //x-coordinate in camera space
            double rayDirX = _comp.State.DirX + _comp.State.PlaneX * cameraX;
            double rayDirY = _comp.State.DirY + _comp.State.PlaneY * cameraX;

            //which box of the map we're in
            int mapX = (int) _comp.State.PosX;
            int mapY = (int) _comp.State.PosY;

            //length of ray from current position to next x or y-side
            double sideDistX;
            double sideDistY;

            //length of ray from one x or y-side to next x or y-side
            double deltaDistX = Math.Abs(1 / rayDirX);
            double deltaDistY = Math.Abs(1 / rayDirY);

            double perpWallDist;

            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;

            bool hit = false; //was there a wall hit?
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
            while (!hit)
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
                    hit = true;
                }
            }

            if (!side)
                perpWallDist = sideDistX - deltaDistX;
            else
                perpWallDist = sideDistY - deltaDistY;

            float lineHeight = (float) (InternalResY / perpWallDist);

            float drawStart = -lineHeight / 2 + InternalResY / 2;

            float drawEnd = drawStart + lineHeight;

            double wallX; //where exactly the wall was hit
            if (!side)
                wallX = _comp.State.PosY + perpWallDist * rayDirY;
            else
                wallX = _comp.State.PosX + perpWallDist * rayDirX;
            wallX -= Math.Floor(wallX); // this leaves just the remainder

            int i = 1;
            while (i < lineHeight)
            {
                var ratio = i / lineHeight;

                var texX = (int) (wallX * 64);
                var texY = (int) Math.Max(64 * ratio, 1);

                var rgb = wallSpan[texX + 64 * (_worldMap[mapX, mapY] - 1) + (texY - 1) * _wallAtlas.Width];

                color = new Color(rgb.R, rgb.G, rgb.B);

                if (side)
                {
                    color.R /= 2;
                    color.G /= 2;
                    color.B /= 2;
                }

                // TODO: This should take into account UI scale Cvars also.
                int scaleIncrementor = 1;
                while (scaleIncrementor <= scaleFactor * 2) // 2 dimensions, so *2
                {
                    vec.X = (x + 1) * scaleFactor + ((int) Math.Ceiling((double) scaleIncrementor / scaleFactor) - 1); // 0 0 1 1; 0 0 0 1 1 1 2 2 2; etc.
                    vec.Y = (drawStart + i) * scaleFactor + scaleIncrementor % scaleFactor; // 1 0 1 0; 1 2 0 1 2 0 1 2 0; etc.

                    if (vec.Y > 0 && vec.Y < InternalResY * scaleFactor)
                        verts.Add(new DrawVertexUV2DColor(vec, color));

                    scaleIncrementor++;
                }

                i++;
            }
        }
        _buffer = verts.ToArray();
    }
}
