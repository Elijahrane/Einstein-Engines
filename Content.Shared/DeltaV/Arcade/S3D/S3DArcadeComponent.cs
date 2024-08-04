using Robust.Shared.Player;

namespace Content.Shared.DeltaV.Arcade.S3D;

[RegisterComponent]
public sealed partial class S3DArcadeComponent : Component
{
    /// <summary>
    /// Current state of the game.
    /// </summary>
    public S3DState State;

    /// <summary>
    /// Current map.
    /// </summary>
    [DataField("mapName")]
    public string MapName = "e1m1.s3d";

    /// <summary>
    /// The walls, etc as an array.
    public int[,,] WorldMap =
    {};
    public float Accumulator = 0f;

    /// <summary>
    /// The player currently playing the active session of S3D.
    /// </summary>
    public ICommonSession? Player = null;

    /// <summary>
    /// The players currently viewing (but not playing) the active session of S3D.
    /// </summary>
    public readonly List<ICommonSession> Spectators = new();
}
