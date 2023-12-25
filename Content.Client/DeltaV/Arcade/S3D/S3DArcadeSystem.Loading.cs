using Content.Client.DeltaV.Arcade.S3D.Renderer;
using Content.Shared.DeltaV.Arcade.S3D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.DeltaV.Arcade.S3D
{
    public sealed partial class S3DArcadeSystem : SharedS3DArcadeSystem
    {
        public S3DRenderer NewRenderer(S3DArcadeComponent component)
        {
            return new S3DRenderer(_resourceCache, component, component.WorldMap, LoadWallAtlas(), LoadFloorAtlas(), LoadCeilingAtlas());
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
    }
}
