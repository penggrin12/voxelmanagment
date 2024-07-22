using System;
using Godot;

namespace Game;

// exists just for chunk rendering without
// duplicating code between player controllers
// upd: now why
public partial class BasePlayer : CharacterBody3D
{
    public World world;
}