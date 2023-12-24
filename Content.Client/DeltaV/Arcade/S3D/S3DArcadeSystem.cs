using Content.Shared.DeltaV.Arcade.S3D;

namespace Content.Client.DeltaV.Arcade.S3D
{
    public sealed class S3DArcadeSystem : SharedS3DArcadeSystem
    {
        private const float _updateRate = 0.03125f;
        private const float _moveSpeed = 0.008f;
        private const float _wallDeadzone = 0.02f;
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
            HandleInput(component);

            component.State.Tick++;
        }

        private void HandleInput(S3DArcadeComponent component)
        {
            if (component.State.Input.HasFlag(InputFlags.Left))
                component.State = Rotate(component.State);

            if (component.State.Input.HasFlag(InputFlags.Right))
                component.State = Rotate(component.State, true);

            if (component.State.Input.HasFlag(InputFlags.Up))
                Move(component);

            if (component.State.Input.HasFlag(InputFlags.Down))
                Move(component, true);
        }

        private S3DState Rotate(S3DState state, bool invert = false)
        {
            var speed = _rotSpeed;
            if (invert)
                speed = -speed;

            double oldDirX = state.DirX;
            state.DirX = state.DirX * Math.Cos(speed) - state.DirY * Math.Sin(speed);
            state.DirY = oldDirX * Math.Sin(speed) + state.DirY * Math.Cos(speed);

            double oldPlaneX = state.PlaneX;
            state.PlaneX = state.PlaneX * Math.Cos(speed) - state.PlaneY * Math.Sin(speed);
            state.PlaneY = oldPlaneX * Math.Sin(speed) + state.PlaneY * Math.Cos(speed);
            return state;
        }

        private void Move(S3DArcadeComponent component, bool invert = false)
        {
            if (!invert)
            {
                if (component.WorldMap[(int) (component.State.PosX + _wallDeadzone + component.State.DirX * _moveSpeed), (int) component.State.PosY] == 0)
                    component.State.PosX += component.State.DirX * _moveSpeed;

                if (component.WorldMap[(int) component.State.PosX, (int) (component.State.PosY + _wallDeadzone + component.State.DirY * _moveSpeed)] == 0)
                    component.State.PosY += component.State.DirY * _moveSpeed;
            }
            else
            {
                if (component.WorldMap[(int) (component.State.PosX - _wallDeadzone - component.State.DirX * _moveSpeed), (int) component.State.PosY] == 0)
                    component.State.PosX -= component.State.DirX * _moveSpeed;

                if (component.WorldMap[(int) component.State.PosX, (int) (component.State.PosY - _wallDeadzone - component.State.DirY * _moveSpeed)] == 0)
                    component.State.PosY -= component.State.DirY * _moveSpeed;
            }
        }
    }
}
