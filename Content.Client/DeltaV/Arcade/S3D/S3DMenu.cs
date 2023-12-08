using Content.Client.DeltaV.Arcade.UI;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.DeltaV.Arcade;

public sealed class S3DMenu : DefaultWindow
{
    private readonly S3DBoundUserInterface _owner;

    public S3DMenu(S3DBoundUserInterface owner)
    {
        _owner = owner;
    }
}
