using Robust.Shared.Player;

namespace Content.Server.DeltaV.Arcade.S3D;

[RegisterComponent]

/// <summary>
/// Unlike other arcade machines, most of the logic is going to be on the client here.
/// The server will still handle saving states and giving them to spectators or letting players
/// continue an already started game.
/// </summary>
public sealed partial class S3DArcadeComponent : Component
{
    /// <summary>
    /// The player currently playing the active session of S3D.
    /// </summary>
    public ICommonSession? Player = null;

    /// <summary>
    /// The players currently viewing (but not playing) the active session of S3D.
    /// </summary>
    public readonly List<ICommonSession> Spectators = new();
}
