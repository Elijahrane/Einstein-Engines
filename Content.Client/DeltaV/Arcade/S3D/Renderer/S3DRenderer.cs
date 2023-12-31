using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Shared.DeltaV.Arcade.S3D;
using Color = Robust.Shared.Maths.Color;
using Robust.Client.ResourceManagement;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Utility;
using Content.Client.Resources;
using Robust.Shared.Graphics;
using System.Linq;

namespace Content.Client.DeltaV.Arcade.S3D.Renderer;

/// <summary>
/// Controls the rendering for the S3D arcade. Logic is in the client's S3DArcadeSystem.cs
/// </summary>
public sealed class S3DRenderer : Control
{
    private readonly IResourceCache _resourceCache;
    private const int InternalResX = 320;
    private const int InternalResY = 240;
    private const float FOV = 66; // note this is derived from PlaneX, but not calculated at run time here because why would we
    private const float ScaleFactor = 2;
    private const float CameraHeight = 0.5f;

    // We do walls in the software rendering style because (1) it takes a few ms even with texture mapping while (2) it makes occlusion way, way, way easier
    // These are colored points
    private DrawVertexUV2DColor[] _wallBuffer = Array.Empty<DrawVertexUV2DColor>();
    private readonly Image<Rgba32> _wallAtlas;
    private S3DArcadeComponent _comp;
    private int[,,] _worldMap;
    private readonly Image<Rgba32> _floorAtlas;
    private readonly Image<Rgba32> _ceilingAtlas;
    // TODO: Could this be just a straight texture? We're not skewing it at all, just scrolling.
    private long _tick = 0;
    public S3DRenderer(IResourceCache resourceCache, S3DArcadeComponent comp, int[,,] worldMap, Image<Rgba32> wallAtlas, Image<Rgba32> floorAtlas, Image<Rgba32> ceilingAtlas)
    {
        _resourceCache = resourceCache;
        _comp = comp;
        _worldMap = worldMap;
        _wallAtlas = wallAtlas;
        _floorAtlas = floorAtlas;
        _ceilingAtlas = ceilingAtlas;
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
            if (watch.ElapsedMilliseconds > 1000 / 20)
            {
                Logger.Warning("Over target! Raycasted in " + watch.ElapsedMilliseconds + " ms");
            }
        }

        _tick = _comp.State.Tick;

        DrawSkybox(handle);

        // There's a size limit of 65532 elements.
        int i = 0;
        while (i < _wallBuffer.Length)
        {
            if (_wallBuffer.Length > i + 65530)
                handle.DrawPrimitives(DrawPrimitiveTopology.PointList, Texture.White, _wallBuffer.AsSpan(i, 65530));
            else
                handle.DrawPrimitives(DrawPrimitiveTopology.PointList, Texture.White, _wallBuffer.AsSpan(i));

            i += 65531;
        }
        DrawFloors(handle);
    }

    private void DrawSkybox(DrawingHandleScreen handle)
    {
        var skybox = _resourceCache.GetTexture("/Textures/DeltaV/Other/S3D/skybox.png");

        float drawEnd = 0.5f * InternalResY * ScaleFactor;

        // we are assuming skyboxes are designed to be equal to InternalResX and InternalResY
        // Shows how much we need to scale the skybox.
        var fovRatio = FOV / 360f;

        var xScale = InternalResX * ScaleFactor;
        var xFOV = skybox.Width * fovRatio;

        var scrollFactor = (float) (new Vector2((float) _comp.State.DirX, (float) _comp.State.DirY).ToAngle().Degrees + 180f) / 360f;
        scrollFactor -= (float) Math.Floor(scrollFactor);

        var scrollOffset = (1 - scrollFactor) * skybox.Width;

        // simple case: we don't go over image boundaries
        if (xFOV + scrollOffset < skybox.Width)
        {
            handle.DrawTextureRectRegion(skybox, new UIBox2(Vector2.Zero, new Vector2(xScale, drawEnd)), new UIBox2(new Vector2(scrollOffset, 0), new Vector2(xFOV + scrollOffset, skybox.Height)));
        }
        // complex case: we have to split into 2 images
        else
        {
            // Image 1: The bit that scrolled over
            var overscroll = Math.Abs(skybox.Width - xFOV - scrollOffset);
            var imgRatio = 1 - overscroll / xFOV;
            handle.DrawTextureRectRegion(skybox, new UIBox2(0, 0, imgRatio * xScale, drawEnd), new UIBox2(new Vector2(skybox.Width - xFOV + overscroll, 0), new Vector2(skybox.Width, skybox.Height)));
            handle.DrawTextureRectRegion(skybox, new UIBox2(imgRatio * xScale, 0, xScale, drawEnd), new UIBox2(Vector2.Zero, new Vector2(overscroll, skybox.Height)));
        }
    }

    /// <summary>
    /// https://en.wikipedia.org/wiki/Cock_and_ball_torture
    /// </summary>
    /// <param name="handle"></param>
    private void DrawFloors(DrawingHandleBase handle)
    {
        DrawVertexUV2DColor[] xDebugBuffer = Array.Empty<DrawVertexUV2DColor>();

        // FINDING X

        // dir vector for center of camera
        var dirVector = new Vector2((float) _comp.State.DirX, (float) _comp.State.DirY);

        // dir vector from our pos to point
        var pointVec = new Vector2((float) (19 - _comp.State.PosX), (float) (12 - _comp.State.PosY)).Normalized();

        var dot = Vector2.Dot(dirVector, pointVec);

        // angle between the two vectors (unsure of handedness yet)
        Angle angle = Math.Acos(dot / dirVector.Length() * pointVec.Length());

        // Find dot product with second vector rotated to tell if it's left or right side of the screen
        var handednessDot = Vector2.Dot(dirVector, new Vector2(pointVec.Y, 0 - pointVec.X));

        // How much of the screen to +- from 0.5
        var screenRatio = angle.Degrees / (FOV / 2);

        var absScreenRatio = handednessDot > 0 ? 0.5 - screenRatio : 0.5 + screenRatio;
        // Finally, Find X;
        float x = (float) absScreenRatio * InternalResX * ScaleFactor;

        handle.DrawLine(new Vector2(x, 0), new Vector2(x, InternalResY * ScaleFactor), Color.Green);

        // FINDING Y
        var dist1Sq = Math.Pow(19f - _comp.State.PosX, 2) + Math.Pow(12f - _comp.State.PosY, 2);

        // based pythagoras
        var realDist = Math.Pow(0.25 + dist1Sq, 0.5);
    }
    private void Raycast()
    {
        // a lot of this is adapted from https://lodev.org/cgtutor/raycasting.html (which is BSD licensed.) Thank you Lode Vandevenne.
        Color color;
        Vector2 vec = Vector2.One;

        var wallSpan = _wallAtlas.GetPixelSpan();
        var floorSpan = _floorAtlas.GetPixelSpan();
        var ceilingSpan = _ceilingAtlas.GetPixelSpan();

        int scaleFactor = 2;
        List<DrawVertexUV2DColor> verts = new List<DrawVertexUV2DColor>();

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
                if (_worldMap[mapX, mapY, 0] > 0)
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

                var rgb = wallSpan[texX + 64 * (_worldMap[mapX, mapY, 0] - 1) + (texY - 1) * _wallAtlas.Width];

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
        _wallBuffer = verts.ToArray();
    }
}
