using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class Hook : KinematicBody2D
{
    public bool TouchingHookable()
    {
        return false;
    }
}