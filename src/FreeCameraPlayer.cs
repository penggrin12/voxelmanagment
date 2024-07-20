namespace Game;

public partial class FreeCameraPlayer : BasePlayer
{
    public override void _Process(double delta)
    {
        HandleUpdateRenderDistance(GetNode<FreeCamera>("Camera3D").Position);
    }
}