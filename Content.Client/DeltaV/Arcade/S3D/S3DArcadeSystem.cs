using Content.Shared.DeltaV.Arcade.S3D;
using Robust.Shared.ContentPack;
using Robust.Client.ResourceManagement;

namespace Content.Client.DeltaV.Arcade.S3D
{
    public sealed partial class S3DArcadeSystem : SharedS3DArcadeSystem
    {
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        private const float UpdateRate = 0.05f;
        private const float MoveSpeed = 0.008f;
        private const float WallDeadzone = 0.02f;
        private const float RotSpeed = 0.005f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // TODO: Only update ones we are tracking on our end.
            var query = EntityQueryEnumerator<S3DArcadeComponent>();

            while (query.MoveNext(out var arcade))
            {
                arcade.Accumulator += frameTime;

                if (arcade.Accumulator > UpdateRate)
                    continue;

                arcade.Accumulator -= UpdateRate;

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
            {
                if (component.State.Input.HasFlag(InputFlags.StrafeMod))
                    Move(component, MoveDirection.Left);
                else if (component.State.Input.HasFlag(InputFlags.SwitchMod))
                {
                    // SwitchWeapons();
                }
                else
                    component.State = Rotate(component.State, false);
            }

            if (component.State.Input.HasFlag(InputFlags.Right))
            {
                if (component.State.Input.HasFlag(InputFlags.StrafeMod))
                    Move(component, MoveDirection.Right);
                else if (component.State.Input.HasFlag(InputFlags.SwitchMod))
                {
                    // SwitchWeapons();
                }
                else
                    component.State = Rotate(component.State, true);
            }

            if (component.State.Input.HasFlag(InputFlags.Up))
                Move(component);

            if (component.State.Input.HasFlag(InputFlags.Down))
                Move(component, MoveDirection.Down);
        }

        private S3DState Rotate(S3DState state, bool invert = false)
        {
            var speed = RotSpeed;
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

        private void Move(S3DArcadeComponent component, MoveDirection dir = MoveDirection.Up)
        {
            switch (dir)
            {
                case MoveDirection.Up:
                    if (component.WorldMap[(int) (component.State.PosX + WallDeadzone + component.State.DirX * MoveSpeed), (int) component.State.PosY] == 0)
                        component.State.PosX += component.State.DirX * MoveSpeed;

                    if (component.WorldMap[(int) component.State.PosX, (int) (component.State.PosY + WallDeadzone + component.State.DirY * MoveSpeed)] == 0)
                        component.State.PosY += component.State.DirY * MoveSpeed;
                    break;
                case MoveDirection.Down:
                    if (component.WorldMap[(int) (component.State.PosX - WallDeadzone - component.State.DirX * MoveSpeed), (int) component.State.PosY] == 0)
                        component.State.PosX -= component.State.DirX * MoveSpeed;

                    if (component.WorldMap[(int) component.State.PosX, (int) (component.State.PosY - WallDeadzone - component.State.DirY * MoveSpeed)] == 0)
                        component.State.PosY -= component.State.DirY * MoveSpeed;
                    break;
                case MoveDirection.Right:
                    if (component.WorldMap[(int) (component.State.PosX + WallDeadzone + component.State.DirY * MoveSpeed), (int) component.State.PosY] == 0)
                        component.State.PosX += component.State.DirY * MoveSpeed;

                    if (component.WorldMap[(int) component.State.PosX, (int) (component.State.PosY + WallDeadzone - component.State.DirX * MoveSpeed)] == 0)
                        component.State.PosY -= component.State.DirX * MoveSpeed;
                    break;
                case MoveDirection.Left:
                    if (component.WorldMap[(int) (component.State.PosX + WallDeadzone - component.State.DirY * MoveSpeed), (int) component.State.PosY] == 0)
                        component.State.PosX -= component.State.DirY * MoveSpeed;

                    if (component.WorldMap[(int) component.State.PosX, (int) (component.State.PosY + WallDeadzone + component.State.DirX * MoveSpeed)] == 0)
                        component.State.PosY += component.State.DirX * MoveSpeed;
                    break;
            }
        }

        private enum MoveDirection : byte
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}
