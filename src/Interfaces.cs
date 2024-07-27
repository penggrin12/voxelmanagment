using System.Threading.Tasks;
using Game.Structs;
using Godot;

namespace Game.Interfaces;

public interface IEntity
{
    public Node3D AsNode3D() { return (Node3D)this; }
}

public interface IPlayer : IEntity
{
    public object GetDebugThingie();
}

public interface IPathfinding
{

}

public interface IPlayerSelectableEntity : IEntity
{
    public void OnPlayerSelect() {}
}

public interface IPlayerControllableEntity : IPlayerSelectableEntity
{
    public void PlayerCommandMove(Location to);
}
