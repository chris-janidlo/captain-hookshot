using System;
using CaptainHookshot.player.grapple_gun.rope;
using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class GrappleGun : Node2D
{
    private static readonly Vector2 LeftAimScale = new(1, -1), RightAimScale = new(1, 1);

    [Export(PropertyHint.Enum, "left,right")]
    private string _controlDirection;

    [Export] private NodePath _hookFlightContainerPath;

    [Export] private float _hookExitSpeed;
    [Export] private int _ropeSegmentCount;

    [Export] private NodePath _hookPath, _barrelPath;

    [Export] private PackedScene _ropeScene;

    private Vector2 _aim;
    private Node2D _barrel, _hookFlightContainer;

    private Rope _currentRope;
    private Hook _hook;

    public override void _Ready()
    {
        _hook = GetNode<Hook>(_hookPath);
        _barrel = GetNode<Node2D>(_barrelPath);
        _hookFlightContainer = GetNode<Node2D>(_hookFlightContainerPath);
    }

    public override void _Process(float delta)
    {
        var dir = GetAimInput();
        if (dir != Vector2.Zero)
        {
            _aim = dir;
            PointAt(_aim);
        }

        var shot = Input.IsActionJustPressed($"shoot_{_controlDirection}");
        if (_aim != Vector2.Zero && shot) Shoot(_aim);
    }

    private Vector2 GetAimInput()
    {
        return Input.GetVector
        (
            $"aim_{_controlDirection}_left",
            $"aim_{_controlDirection}_right",
            $"aim_{_controlDirection}_up",
            $"aim_{_controlDirection}_down"
        ).Normalized();
    }

    private void Shoot(Vector2 direction)
    {
        if (_currentRope != null) throw new NotImplementedException("re-shooting is not implemented");

        _currentRope = _ropeScene.Instance<Rope>();
        _currentRope.Init(_ropeSegmentCount, direction);
        _barrel.AddChild(_currentRope);

        _hook.GetParent().RemoveChild(_hook);
        _hook.Scale = Vector2.One;
        _hookFlightContainer.AddChild(_hook);
        _currentRope.AttachEndTo(_hook);

        _hook.Velocity = direction * _hookExitSpeed;
    }

    private void PointAt(Vector2 direction)
    {
        LookAt(GlobalPosition + direction);

        Scale = direction.x < 0
            ? LeftAimScale
            : RightAimScale;
    }
}