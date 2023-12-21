using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Shared.DeltaV.Arcade.S3D;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.DeltaV.Arcade.S3D.Renderer;

/// <summary>
/// Controls the rendering for the S3D arcade. Logic is in the client's S3DArcadeSystem.cs
/// </summary>
public sealed class S3DRenderer : Control
{
    /// <summary>
    /// Buffer for walls.
    /// </summary>
    private DrawVertexUV2DColor[] _buffer = Array.Empty<DrawVertexUV2DColor>();
    private S3DArcadeComponent _comp;
    private int[,] _worldMap;
    public S3DRenderer(S3DArcadeComponent comp, int[,] worldMap)
    {
        _comp = comp;
        _worldMap = worldMap;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // TODO: As game logic is locked to 30 fps, we don't need to draw if there hasn't been a frame update.

        Raycast();

        var values = new ReadOnlySpan<DrawVertexUV2DColor>(_buffer);

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, Texture.White, values);
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
            int side = 0; //was a NS or a EW wall hit?
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
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }
                //Check if ray has hit a wall
                if (_worldMap[mapX, mapY] > 0)
                {
                    hit = 1;
                }
            }

            if (side == 0)
                perpWallDist = sideDistX - deltaDistX;
            else
                perpWallDist = sideDistY - deltaDistY;

            float lineHeight = (float) (Size.Y / perpWallDist);

            // the center should be at the center of the screen.
            float drawStart = -lineHeight / 2 + Size.Y / 2;
            if (drawStart < 0) drawStart = 0;
            float drawEnd = lineHeight / 2 + Size.Y / 2;
            if (drawEnd >= Size.Y) drawEnd = Size.Y - 1;

            //choose wall color
            Color color;
            switch (_worldMap[mapX, mapY])
            {
                case 1: color = Color.Red; break; //red
                case 2: color = Color.Green; break; //green
                case 3: color = Color.Blue; break; //blue
                case 4: color = Color.White; break; //white
                default: color = Color.Yellow; break; //yellow
            }

            if (side == 1)
            {
                color = Color.FromSrgb(new Color(color.R / 2, color.G / 2, color.B / 2, 1));
            }

            /// Give 2 coordinates: Where to start drawing and where to end.
            verts.Add(new DrawVertexUV2DColor(new Vector2(x + 1, drawStart), color)); // x
            verts.Add(new DrawVertexUV2DColor(new Vector2(x + 1, drawEnd), color)); // y
        }
        _buffer = verts.ToArray();
    }
}
