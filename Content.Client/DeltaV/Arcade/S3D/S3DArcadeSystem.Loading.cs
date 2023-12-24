using Content.Shared.DeltaV.Arcade.S3D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.DeltaV.Arcade.S3D
{
    public sealed partial class S3DArcadeSystem : SharedS3DArcadeSystem
    {
        /// <summary>
        /// We need to load the texture into CPU memory because it's orders of magnitude faster if we are going to be accessing
        /// thousands of texels individually. Texture.GetPixel is so unperformant it's kind of useless unless you need a single
        /// pixel once. It's not Clyde's fault really - raycasters are really only suited to software (i.e. CPU) rendering.
        /// </summary>
        public Image<Rgba32> LoadWallAtlas()
        {
            if (!_resourceManager.TryContentFileRead("/Textures/DeltaV/Other/S3D/atlas.png", out var stream))
            {
                Logger.Error("Failed to load wall atlas for S3D!");
                return new Image<Rgba32>(512, 64);
            }

            return Image.Load<Rgba32>(stream);
        }
    }
}
