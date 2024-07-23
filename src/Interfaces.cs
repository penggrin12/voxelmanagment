using Godot;

namespace Game.Interfaces;

public interface IPlayer
{
    public void SetWorld(World world);
    
    public Node3D AsNode3D();
}
