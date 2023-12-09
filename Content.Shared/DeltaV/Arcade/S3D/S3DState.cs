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

    public S3DState(double posX, double posY, double dirX, double dirY, double planeX, double planeY)
    {
        PosX = posX;
        PosY = posY;
        DirX = dirX;
        DirY = dirY;
        PlaneX = planeX;
        PlaneY = planeY;
    }
}
