namespace Content.Client.DeltaV.Arcade.UI;

public sealed class S3DBoundUserInterface : BoundUserInterface
{
    private S3DMenu? _menu;
    public S3DBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new S3DMenu(this);
        _menu.OpenCentered();
    }
}
