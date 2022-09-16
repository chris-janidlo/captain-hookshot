using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class HookableArea : Node
{
    private void OnBodyEntered(Node body)
    {
        ((Hook)body).EnteredHookableArea();
    }

    public void OnBodyExited(Node body)
    {
        ((Hook)body).ExitedHookableArea();
    }
}