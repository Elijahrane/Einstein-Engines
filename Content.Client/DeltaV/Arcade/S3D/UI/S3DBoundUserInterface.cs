namespace Content.Client.DeltaV.Arcade.S3D.UI;
using Content.Shared.DeltaV.Arcade.S3D;
using Content.Shared.Input;
using Robust.Shared.Input;

public sealed class S3DBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private S3DMenu? _menu;
    private S3DArcadeComponent? _comp = null;
    public S3DBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        if (_entMan.TryGetComponent<S3DArcadeComponent>(owner, out var comp))
            _comp = comp;
    }

    protected override void Open()
    {
        base.Open();

        _menu = new S3DMenu(this);
        _menu.OpenCentered();
    }

    public void RegisterKeyPress(BoundKeyFunction function)
    {
        if (_comp == null)
            return;

        if (function == ContentKeyFunctions.ArcadeLeft && !_comp.State.Input.HasFlag(InputFlags.Left))
        {
            // if you know a way to remove the stupid casting here, please let me know.
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.Left);
        }
        else if (function == ContentKeyFunctions.ArcadeRight && !_comp.State.Input.HasFlag(InputFlags.Right))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.Right);
        }
        else if (function == ContentKeyFunctions.ArcadeUp && !_comp.State.Input.HasFlag(InputFlags.Up))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.Up);
        }
        else if (function == ContentKeyFunctions.ArcadeDown && !_comp.State.Input.HasFlag(InputFlags.Down))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.Down);
        }
        else if (function == ContentKeyFunctions.Arcade1 && !_comp.State.Input.HasFlag(InputFlags.Fire))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.Fire);
        }
        else if (function == ContentKeyFunctions.Arcade2 && !_comp.State.Input.HasFlag(InputFlags.StrafeMod))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.StrafeMod);
        }
        else if (function == ContentKeyFunctions.Arcade3 && !_comp.State.Input.HasFlag(InputFlags.SwitchMod))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input + (int) InputFlags.SwitchMod);
        }
    }

    public void UnregisterKeyPress(BoundKeyFunction function)
    {
        if (_comp == null)
            return;

        if (function == ContentKeyFunctions.ArcadeLeft && _comp.State.Input.HasFlag(InputFlags.Left))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.Left);
        }
        else if (function == ContentKeyFunctions.ArcadeRight && _comp.State.Input.HasFlag(InputFlags.Right))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.Right);
        }
        else if (function == ContentKeyFunctions.ArcadeUp && _comp.State.Input.HasFlag(InputFlags.Up))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.Up);
        }
        else if (function == ContentKeyFunctions.ArcadeDown && _comp.State.Input.HasFlag(InputFlags.Down))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.Down);
        }
        else if (function == ContentKeyFunctions.Arcade1 && _comp.State.Input.HasFlag(InputFlags.Fire))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.Fire);
        }
        else if (function == ContentKeyFunctions.Arcade2 && _comp.State.Input.HasFlag(InputFlags.StrafeMod))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.StrafeMod);
        }
        else if (function == ContentKeyFunctions.Arcade3 && _comp.State.Input.HasFlag(InputFlags.SwitchMod))
        {
            _comp.State.Input = (InputFlags) ((int) _comp.State.Input - (int) InputFlags.SwitchMod);
        }
    }
}
