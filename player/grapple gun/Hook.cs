using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class Hook : KinematicBody2D
{
    public Vector2 Velocity;

    public override void _PhysicsProcess(float delta)
    {
        MoveAndSlide(Velocity);
        LookAt(GlobalPosition + Velocity);
    }
}