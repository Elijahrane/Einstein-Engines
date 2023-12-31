using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Arcade.S3D;

[Serializable, NetSerializable]
public struct S3DState
{
    public double PosX;
    public double PosY;
    public double DirX;
    public double DirY;
    public double PlaneX;
    public double PlaneY;
    public InputFlags Input;
    public long Tick;
    public S3DState(double posX, double posY, double dirX, double dirY, double planeX, double planeY, InputFlags input, long tick = 1)
    {
        PosX = posX;
        PosY = posY;
        DirX = dirX;
        DirY = dirY;
        PlaneX = planeX;
        PlaneY = planeY;
        Input = input;
        Tick = tick;
    }
}

/// <summary>
/// The arcade keys pressed down.
/// </summary>
[Flags]
public enum InputFlags : byte
{
    None = 0,
    Up = 1,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Fire = 1 << 4,
    Use = 1 << 5,
    StrafeMod = 1 << 6,
    SwitchMod = 1 << 7
}
