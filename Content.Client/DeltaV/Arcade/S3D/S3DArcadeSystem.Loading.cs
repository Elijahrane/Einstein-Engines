using Content.Client.DeltaV.Arcade.S3D.Renderer;
using Content.Shared.DeltaV.Arcade.S3D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.DeltaV.Arcade.S3D
{
    public sealed partial class S3DArcadeSystem : SharedS3DArcadeSystem
    {
        public S3DRenderer NewRenderer(S3DArcadeComponent component)
        {
            return new S3DRenderer(_resourceCache, component, component.WorldMap, LoadWallAtlas(), LoadFloorAtlas(), LoadCeilingAtlas(), LoadSkybox());
        }

        /// <summary>
        /// Loads the map from disk.
        /// Path should be relative to the S3D maps folder.
        /// WARNING: this function looks awful because sandbox blocks basically every useful lib for this stuff
        /// </summary>
        private bool LoadWorldMap(string name, [NotNullWhen(true)] out int[,,]? worldMap)
        {
            worldMap = null;

            string relativePath = "/Textures/DeltaV/Other/S3D/" + name;

            if (!_resourceManager.TryContentFileRead(relativePath, out var stream))
            {
                Logger.Error("Failed to open stream for " + relativePath);
                return false;
            }

            string? txt = null;

            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                txt = reader.ReadToEnd();
            }

            if (txt == null || txt == string.Empty)
            {
                Logger.Error("S3D Map " + relativePath + " appears empty.");
                worldMap = null;
                return false;
            }

            string[] lines = txt.Split("@", StringSplitOptions.None);

            // Get array dimensions
            // StringReader isn't allowed in sandbox so time for some shitcode
            int x = 0;
            bool foundX = false;
            while (!foundX)
            {
                if (txt[x] != '@')
                    x++;
                else
                    foundX = true;
            }

            int y = 0;
            bool foundY = false;
            while (!foundY)
            {
                foreach (var line in lines)
                {
                    if (Char.IsNumber(line[^1]))
                        y++;
                    else
                        foundY = true;
                }
            }


            // Initialize array
            worldMap = new int[x, y, 3];

            // fill out array from text
            for (int i = 0; i < y; i++)
            {
                // I have no idea what the newline looks like and sandbox blocks me from standardizing it so, we'll reverse and work backwards
                var row = Enumerable.Reverse(lines[i]);

                for (int rowI = 0; rowI < x; rowI++)
                {
                    // this is easily the worst bit of code I have written in quite a long time
                    worldMap[i, (x - 1) - rowI, 0] = Int32.Parse(row.ElementAt(rowI).ToString());
                }
            }

            return true;
        }

        /// <summary>
        /// We need to load the texture into CPU memory because it's orders of magnitude faster if we are going to be accessing
        /// thousands of texels individually. Texture.GetPixel is so unperformant it's kind of useless unless you need a single
        /// pixel once. It's not Clyde's fault really - raycasters are really only suited to software (i.e. CPU) rendering.
        /// </summary>
        private Image<Rgba32> LoadWallAtlas()
        {
            if (!_resourceManager.TryContentFileRead("/Textures/DeltaV/Other/S3D/wall_atlas.png", out var stream))
            {
                Logger.Error("Failed to load wall atlas for S3D!");
                return new Image<Rgba32>(512, 64);
            }

            return Image.Load<Rgba32>(stream);
        }

        private Image<Rgba32> LoadFloorAtlas()
        {
            if (!_resourceManager.TryContentFileRead("/Textures/DeltaV/Other/S3D/floor_atlas.png", out var stream))
            {
                Logger.Error("Failed to load floor atlas for S3D!");
                return new Image<Rgba32>(32, 32);
            }

            return Image.Load<Rgba32>(stream);
        }

        private Image<Rgba32> LoadCeilingAtlas()
        {
            if (!_resourceManager.TryContentFileRead("/Textures/DeltaV/Other/S3D/ceiling_atlas.png", out var stream))
            {
                Logger.Error("Failed to load ceiling atlas for S3D!");
                return new Image<Rgba32>(32, 32);
            }

            return Image.Load<Rgba32>(stream);
        }

        private Image<Rgba32> LoadSkybox()
        {
            if (!_resourceManager.TryContentFileRead("/Textures/DeltaV/Other/S3D/skybox.png", out var stream))
            {
                Logger.Error("Failed to load skybox for S3D!");
                return new Image<Rgba32>(320, 120);
            }

            return Image.Load<Rgba32>(stream);
        }
    }
}
