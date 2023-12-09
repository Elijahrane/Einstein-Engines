namespace Content.Shared.DeltaV.Arcade.S3D;

public abstract class SharedS3DArcadeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<S3DArcadeComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, S3DArcadeComponent component, ComponentInit args)
    {
        component.State = new S3DState(22, 12, -1, 0, 0, 0.66);
    }
}
