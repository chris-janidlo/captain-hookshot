using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class Hook : KinematicBody2D
{
    public bool TouchingHookable { get; private set; }

    public void EnteredHookableArea()
    {
        TouchingHookable = true;
    }

    public void ExitedHookableArea()
    {
        TouchingHookable = false;
    }
}