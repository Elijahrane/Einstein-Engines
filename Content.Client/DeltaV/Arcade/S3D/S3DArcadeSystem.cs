using Content.Shared.Access.Components;
using Content.Shared.DeltaV.Arcade.S3D;

namespace Content.Client.DeltaV.Arcade.S3D
{
    public sealed class S3DArcadeSystem : SharedS3DArcadeSystem
    {
        private const float _updateRate = 0.03125f;
        private const float _rotSpeed = 0.005f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // TODO: Only update ones we are tracking on our end.
            var query = EntityQueryEnumerator<S3DArcadeComponent>();

            while (query.MoveNext(out var arcade))
            {
                arcade.Accumulator += frameTime;

                if (arcade.Accumulator > _updateRate)
                    continue;

                arcade.Accumulator -= _updateRate;

                RunTick(arcade);
            }
        }

        private void RunTick(S3DArcadeComponent component)
        {
            double oldDirX = component.State.DirX;
            component.State.DirX = component.State.DirX * Math.Cos(_rotSpeed) - component.State.DirY * Math.Sin(_rotSpeed);
            component.State.DirY = oldDirX * Math.Sin(_rotSpeed) + component.State.DirY * Math.Cos(_rotSpeed);

            double oldPlaneX = component.State.PlaneX;
            component.State.PlaneX = component.State.PlaneX * Math.Cos(_rotSpeed) - component.State.PlaneY * Math.Sin(_rotSpeed);
            component.State.PlaneY = oldPlaneX * Math.Sin(_rotSpeed) + component.State.PlaneY * Math.Cos(_rotSpeed);
        }

    }
}
