using CaptainHookshot.player.grapple_gun;
using Godot;

namespace CaptainHookshot.player;

public class Player : Node2D
{
    [Export] private float _gravityAccel, _drag;
    [Export] private NodePath _leftGunPath, _rightGunPath, _bodyPath;

    private KinematicBody2D _body;

    private GrappleGun _leftGun, _rightGun;
    private Vector2 _velocity;

    public override void _Ready()
    {
        _leftGun = GetNode<GrappleGun>(_leftGunPath);
        _rightGun = GetNode<GrappleGun>(_rightGunPath);

        _body = GetNode<KinematicBody2D>(_bodyPath);
    }

    public override void _PhysicsProcess(float delta)
    {
        var oldVelocity = _velocity;

        _velocity +=
            _leftGun.PullAcceleration +
            _rightGun.PullAcceleration +
            _gravityAccel * delta * Vector2.Down;

        // separate step so that drag is always up to date
        _velocity -= _velocity.Normalized() * _velocity.LengthSquared() * _drag * delta;

        _body.MoveAndCollide(_velocity * delta);

        var report = new PhysicsReport
        {
            Position = _body.GlobalPosition,
            Velocity = _velocity,
            Acceleration = oldVelocity - _velocity
        };

        _leftGun.UpdatePhysicsReport(report);
        _rightGun.UpdatePhysicsReport(report);
    }
}